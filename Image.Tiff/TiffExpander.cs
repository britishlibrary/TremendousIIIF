using System;
using SkiaSharp;
using T = BitMiracle.LibTiff.Classic;
using Image.Common;
using System.Runtime.InteropServices;
using System.Net.Http;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace Image.Tiff
{
    public class TiffExpander
    {
        public static async Task<(ProcessState state, SKImage image)> ExpandRegion(HttpClient httpClient, ILogger log, Uri imageUri, ImageRequest request, bool allowSizeAboveFull)
        {
            if (imageUri.IsFile)
            {
                using (var tiff = T.Tiff.Open(imageUri.LocalPath, "r"))
                {
                    return ReadFullImage(tiff, request, allowSizeAboveFull);
                }
            }
            else
            {
                var stream = new TiffHttpSource(httpClient, log, imageUri, request.RequestId);
                await stream.Initialise();
                using (var tiff = T.Tiff.ClientOpen("custom", "r", null, stream))
                {
                    return ReadFullImage(tiff, request, allowSizeAboveFull);
                }
            }
        }

        public static Metadata GetMetadata(HttpClient httpClient, ILogger log, Uri imageUri, int defaultTileWidth, string requestId)
        {
            if (imageUri.IsFile)
            {
                if (!File.Exists(imageUri.LocalPath))
                {
                    throw new FileNotFoundException();
                }
                using (var tiff = T.Tiff.Open(imageUri.LocalPath, "r"))
                {
                    return ReadMetadata(tiff, defaultTileWidth);
                }
            }
            else
            {
                var stream = new TiffHttpSource(httpClient, log, imageUri, requestId);
                using (var tiff = T.Tiff.ClientOpen("custom", "r", null, stream))
                {
                    return ReadMetadata(tiff, defaultTileWidth);
                }
            }
        }

        private static Metadata ReadMetadata(T.Tiff tiff, int defaultTileWidth)
        {
            var x = tiff.GetField(T.TiffTag.IMAGEWIDTH);
            int width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();
            var twtag = tiff.GetField(T.TiffTag.TILEWIDTH);

            var tileWidth = twtag == null ? defaultTileWidth : twtag[0].ToInt();

            return new Metadata
            {
                Width = width,
                Height = height,
                TileWidth = tileWidth,
                ScalingLevels = (int)(Math.Floor(Math.Log(Math.Max(width, height), 2)) - 3)
            };
        }

        private static (ProcessState state, SKImage image) ReadFullImage(T.Tiff tiff, in ImageRequest request, bool allowSizeAboveFull)
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
                xres = xres / 0.0254;
                yres = yres / 0.0254;
            }

            var isTileable = tiff.IsTiled();
            var state = ImageRequestInterpreter.GetInterpretedValues(request, width, height, allowSizeAboveFull);
            state.HorizontalResolution = Convert.ToUInt16(xres);
            state.VerticalResolution = Convert.ToUInt16(yres);
            var raster = new int[width * height];
            if (!tiff.ReadRGBAImageOriented(width, height, raster, T.Orientation.TOPLEFT))
            {
                throw new IOException("Unable to decode TIFF file");
            }
            using (var bmp = CreateBitmapFromPixels(raster, width, height))
            {
                var desiredWidth = Math.Max(1, (int)Math.Round(state.RegionWidth * state.ImageScale));
                var desiredHeight = Math.Max(1, (int)Math.Round(state.RegionHeight * state.ImageScale));
                Log.Debug("Desired size {@DesiredWidth}, {@DesiredHeight}", desiredWidth, desiredHeight);

                var regionWidth = state.RegionWidth;
                var regionHeight = state.RegionHeight;

                var srcRegion = SKRectI.Create(state.StartX, state.StartY, regionWidth, regionHeight);
                return (state, CopyBitmapRegion(bmp, desiredWidth, desiredHeight, srcRegion));
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
