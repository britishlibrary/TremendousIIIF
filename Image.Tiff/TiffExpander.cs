using System;
using SkiaSharp;
using T = BitMiracle.LibTiff.Classic;
using Image.Common;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Image.Tiff
{
    public class TiffExpander
    {
        private static readonly T.TiffErrorHandler _errorHandler = new QuietErrorHandler();

        public static (ProcessState state, SKImage image) ExpandRegion(Stream stream, ILogger log, Uri imageUri, ImageRequest request, bool allowSizeAboveFull)
        {
            T.Tiff.SetErrorHandler(_errorHandler);
            if (imageUri.IsFile)
            {
                using (var tiff = T.Tiff.Open(imageUri.LocalPath, "r"))
                {
                    return ReadFullImage(log, tiff, request, allowSizeAboveFull);
                }
            }
            else
            {
                using (var tiff = T.Tiff.ClientOpen("custom", "r", stream, new T.TiffStream()))
                {
                    return ReadFullImage(log, tiff, request, allowSizeAboveFull);
                }
            }
        }

        public static Metadata GetMetadata(Stream stream, ILogger log, Uri imageUri, int defaultTileWidth)
        {
            T.Tiff.SetErrorHandler(_errorHandler);
            if (imageUri.IsFile)
            {
                using (var tiff = T.Tiff.Open(imageUri.LocalPath, "r"))
                {
                    return ReadMetadata(tiff, defaultTileWidth);
                }
            }
            else
            {
                using (var tiff = T.Tiff.ClientOpen("custom", "r", stream, new T.TiffStream()))
                {
                    return ReadMetadata(tiff, defaultTileWidth);
                }
            }
        }

        private static Metadata ReadMetadata(T.Tiff tiff, int defaultTileWidth)
        {
            int width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();
            var twtag = tiff.GetField(T.TiffTag.TILEWIDTH);
            var tltag = tiff.GetField(T.TiffTag.TILELENGTH);

            var tileWidth = twtag == null ? defaultTileWidth : twtag[0].ToInt();
            var tileHeight = tltag == null ? defaultTileWidth : tltag[0].ToInt();
            var GeoKeyDirectoryTag = tiff.GetField((T.TiffTag)34735);

            var sub_wh = new List<(int, int)>();

            for (var count = 0; tiff.ReadDirectory(); count++)
            {
                int sub_width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
                int sub_height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();
                sub_wh.Add((sub_width, sub_height));
            }

            return new Metadata
            {
                Width = width,
                Height = height,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                ScalingLevels = (int)(Math.Floor(Math.Log(Math.Max(width, height), 2)) - 3),
                HasGeoData = GeoKeyDirectoryTag != null,
                Sizes = sub_wh
            };
        }

        private static (ProcessState state, SKImage image) ReadFullImage(ILogger log, T.Tiff tiff, in ImageRequest request, bool allowSizeAboveFull)
        {
            int width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();

            var restag = tiff.GetField(T.TiffTag.RESOLUTIONUNIT);
            var xrestag = tiff.GetField(T.TiffTag.XRESOLUTION);
            var yrestag = tiff.GetField(T.TiffTag.YRESOLUTION);

            var resunit = restag == null ? 2 : restag[0].ToShort();
            var xres = xrestag == null ? 96 : xrestag[0].ToDouble();
            var yres = yrestag == null ? 96 : yrestag[0].ToDouble();

            // pixels per metre
            if (resunit == 3)
            {
                xres /= 0.0254;
                yres /= 0.0254;
            }

            var state = ImageRequestInterpreter.GetInterpretedValues(request, width, height, allowSizeAboveFull);
            state.HorizontalResolution = Convert.ToUInt16(xres);
            state.VerticalResolution = Convert.ToUInt16(yres);

            // TODO: if tiled/striped, calculate how many tiles needed to satisfy region request and convert that to RGB.
            // unless it's full region, in which case current method probably faster. benchmark!

            // TODO: find which sub image if available best satisfies the resolution request

            if ((width == state.RegionWidth && height == state.RegionHeight) || !tiff.IsTiled())
            {
                int[] raster = new int[width * height];
                if (!tiff.ReadRGBAImageOriented(width, height, raster, T.Orientation.TOPLEFT))
                {
                    throw new IOException("Unable to decode TIFF file");
                }

                using (var bmp = CreateBitmapFromPixels(raster, width, height))
                {
                    var desiredWidth = Math.Max(1, (int)Math.Round(state.RegionWidth * state.ImageScale));
                    var desiredHeight = Math.Max(1, (int)Math.Round(state.RegionHeight * state.ImageScale));
                    log.LogDebug("Desired size {@DesiredWidth}, {@DesiredHeight}", desiredWidth, desiredHeight);

                    var regionWidth = state.RegionWidth;
                    var regionHeight = state.RegionHeight;

                    var srcRegion = SKRectI.Create(state.StartX, state.StartY, regionWidth, regionHeight);
                    return (state, CopyBitmapRegion(bmp, desiredWidth, desiredHeight, srcRegion));
                }
            }
            // try and composit from tiles
            else
            {

                var tw = tiff.GetField(T.TiffTag.TILEWIDTH)[0].ToInt();
                var th = tiff.GetField(T.TiffTag.TILELENGTH)[0].ToInt();
                var rem_x = state.RegionWidth % tw;
                var rem_y = state.RegionHeight % th;

                // +-----+-----+-----+-----+-----+
                // |     |     |     |     |     |
                // |     |     |     |     |     |
                // |     |     |     |     |     |
                // +-----------------------------+
                // |     | +-------------+ |     |
                // |     | |xxxxxxxxxxxxx| |     |
                // |     | |xxxxxxxxxxxxx| |     |
                // +-------+xxxxxxxxxxxxx+-------+
                // |     | |xxxxxxxxxxxxx| |     |
                // |     | |xxxxxxxxxxxxx| |     |
                // |     | +-------------+ |     |
                // +-----+-----------------+-----+


                // locate the region within tiles, extract and composite the tiles, then clip to requested area
                // because it might not be on a tile boundary

                int tiles_needed_start_x = (int)Math.Floor((double)state.StartX / tw);
                int tiles_needed_end_x = (int)Math.Ceiling((double)(state.StartX + state.RegionWidth) / tw);

                int tiles_needed_start_y = (int)Math.Floor((double)state.StartY / tw);
                int tiles_needed_end_y = (int)Math.Ceiling((double)(state.StartY + state.RegionHeight) / tw);

                var needed_tiles_x = tiles_needed_end_x + (rem_x == 0 ? 0 : 1) - tiles_needed_start_x;
                var needed_tiles_y = tiles_needed_end_y + (rem_y == 0 ? 0 : 1) - tiles_needed_start_y;
                log.LogDebug("Requested TIFF Tiles {@StartX} {@EndX} {@StartY} {@EndY}", tiles_needed_start_x, tiles_needed_end_x, tiles_needed_start_y, tiles_needed_end_y);

                int[,][] raster = new int[needed_tiles_x + 1, needed_tiles_y + 1][];

                //var rgbimg = TiffRgbaImage.Create(tiff, false, out var errorMsg);
                //rgbimg.ReqOrientation = Orientation.TOPLEFT;

                for (var tx = 0; tx <= needed_tiles_x; tx++)
                {
                    for (var ty = 0; ty <= needed_tiles_y; ty++)
                    {
                        raster[tx, ty] = new int[tw * th];
                        int col = tx * tw + (tw * tiles_needed_start_x);
                        int row = ty * th + (th * tiles_needed_start_y);

                        var result = tiff.ReadRGBATile(col, row, raster[tx, ty]);
                        if (!result)
                        {
                            var x = result;
                        }
                    }
                }

                using (var tiled_surface = SKSurface.Create(new SKImageInfo(width: needed_tiles_x * tw, height: needed_tiles_y * th, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul)))
                using (var canvas = tiled_surface.Canvas)
                using (var region = new SKRegion())
                using (var paint = new SKPaint() { FilterQuality = SKFilterQuality.High})
                {
                    // draw each tile into the surface at the right place
                    for (var y = 0; y <= needed_tiles_y; y++)
                    {
                        // flip over y axis
                        canvas.Translate(0, y * th);
                        canvas.Scale(1, -1, 0, 0);

                        for (var x = 0; x <= needed_tiles_x; x++)
                        {
                            var bmp = CreateBitmapFromPixels(raster[x, y], tw, th);
                            // y axis is flipped and translated so 0 should be tile hight * row.
                            // so we can just draw to 0 on the y axis
                            var point = new SKPoint(x * tw, 0);
                            canvas.DrawBitmap(bmp, point, paint);
                        }
                        // translate calls are cumulative
                        // should benchmark to see if it's worth doing the mental arithmatic or just callin greset for every row
                        canvas.ResetMatrix();
                    }

                    // set the clip region, because we might not be on tile boundaries
                    // if start == tw, don't add it
                    var rect = new SKRectI((tiles_needed_start_x * tw) + state.StartX == tw? 0: state.StartX, (tiles_needed_start_y * th) + state.StartY == th? 0 : state.StartY, state.RegionWidth + (state.StartX == tw ? 0 : state.StartX), state.RegionHeight + (state.StartY == th ? 0 : state.StartY));
                    return (state, tiled_surface.Snapshot().Subset(rect));
                }
            }


        }

        public static SKBitmap CreateBitmapFromPixels(int[] pixelData, int width, int height)
        {
            var bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            GCHandle pinnedArray = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();
            bmp.SetPixels(pointer);
            pinnedArray.Free();
            return bmp;
        }

        // Current benchmark winner
        public static SKImage CopyBitmapRegion(SKBitmap bmp, int width, int height, SKRectI srcRegion)
        {
            using (var output = new SKBitmap(width, height))
            {
                bmp.ExtractSubset(output, srcRegion);
                return SKImage.FromBitmap(output);
            }
        }


        // Benchmarking indicates using SKBitmap.ExtractSubset() -> SKImage -> SKBitmap -> SKImage (basically a copy) is faster
        public static SKImage CopyImageRegion(SKImage srcImage, int width, int height, SKRectI srcRegion)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(width: width, height: height, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul)))
            using (var paint = new SKPaint())
            {
                var canvas = surface.Canvas;
                paint.FilterQuality = SKFilterQuality.High;
                canvas.DrawImage(srcImage, srcRegion, new SKRect(0, 0, width, height), paint);
                return surface.Snapshot();
            }
        }

        public static SKImage CopyImageRegion2(SKBitmap srcImage, int width, int height, SKRectI srcRegion)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(width: width, height: height, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul)))
            using (var output = new SKBitmap(width, height))
            using (var paint = new SKPaint())
            {
                paint.FilterQuality = SKFilterQuality.High;
                var canvas = surface.Canvas;
                srcImage.ExtractSubset(output, srcRegion);
                canvas.DrawBitmap(output, new SKRect(0, 0, output.Width, output.Height), new SKRect(0, 0, width, height), paint);
                return surface.Snapshot();
            }
        }

        public static SKImage CopyImageRegion3(SKBitmap srcImage, int width, int height, SKRectI srcRegion)
        {
            using (var output = new SKBitmap(width, height))
            {
                srcImage.ScalePixels(output, SKFilterQuality.High);
                return SKImage.FromBitmap(output);
            }

        }

        public static SKImage CopyImageRegion4(SKBitmap srcImage, int width, int height, SKRectI srcRegion)
        {
            var result = srcImage.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
            return SKImage.FromBitmap(result);
        }
    }
}

