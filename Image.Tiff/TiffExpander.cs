using System;
using SkiaSharp;
using T = BitMiracle.LibTiff.Classic;
using Image.Common;
using System.Runtime.InteropServices;
using System.Net.Http;
using Serilog;
using System.IO;

namespace Image.Tiff
{
    public class TiffExpander
    {
        public static (ProcessState state, SKImage image) ExpandRegion(HttpClient httpClient, ILogger log, Uri imageUri, ImageRequest request, bool allowSizeAboveFull)
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
                var stream = new TiffSource(httpClient, log, imageUri, request.RequestId);
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
                using (var tiff = T.Tiff.Open(imageUri.LocalPath, "r"))
                {
                    return ReadMetadata(tiff, defaultTileWidth);
                }
            }
            else
            {
                var stream = new TiffSource(httpClient, log, imageUri, requestId);
                using (var tiff = T.Tiff.ClientOpen("custom", "r", null, stream))
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

            var tileWidth = twtag == null ? defaultTileWidth : twtag[0].ToInt();

            return new Metadata
            {
                Width = width,
                Height = height,
                TileWidth = tileWidth,
                ScalingLevels = (int)(Math.Floor(Math.Log(Math.Max(width, height), 2)) - 3)
            };
        }

        private static (ProcessState state, SKImage image) ReadFullImage(T.Tiff tiff, ImageRequest request, bool allowSizeAboveFull)
        {
            int width = tiff.GetField(T.TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(T.TiffTag.IMAGELENGTH)[0].ToInt();

            var isTileable = tiff.IsTiled();
            var state = ImageRequestInterpreter.GetInterpretedValues(request, width, height, allowSizeAboveFull);
            var raster = new int[width * height];
            if (!tiff.ReadRGBAImageOriented(width, height, raster, T.Orientation.TOPLEFT))
            {
                throw new IOException("Unable to decode TIFF file");
            }
            using (var bmp = CreateBitmapFromPixels(raster, width, height))
            {
                var desiredWidth = Math.Max(1, (int)Math.Round(state.TileWidth * state.ImageScale));
                var desiredHeight = Math.Max(1, (int)Math.Round(state.TileHeight * state.ImageScale));
                Log.Debug("Desired size {@desiredWidth}, {@desiredHeight}", desiredWidth, desiredHeight);

                var regionWidth = (int)Math.Round((state.TileWidth / state.OutputScale) * state.ImageScale);
                var regionHeight = (int)Math.Round((state.TileHeight / state.OutputScale) * state.ImageScale);

                var srcRegion = SKRectI.Create(state.StartX, state.StartY, regionWidth, regionHeight);
                return (state, CopyImageRegion2(bmp, desiredWidth, desiredHeight, srcRegion));
            }
        }

        public static SKBitmap CreateBitmapFromPixels(int[] pixelData, int width, int height)
        {
            var bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            bmp.LockPixels();
            GCHandle pinnedArray = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();
            bmp.SetPixels(pointer);
            pinnedArray.Free();
            bmp.UnlockPixels();
            return bmp;
        }

        public static SKImage CopyBitmapRegion(SKBitmap bmp, int width, int height, SKRectI srcRegion)
        {
            using (var output = new SKBitmap(width, height))
            {
                bmp.ExtractSubset(output, srcRegion);
                var img = SKImage.FromBitmap(output);
                return SKImage.FromBitmap(SKBitmap.FromImage(img));
            }
        }

        public static SKImage CopyBitmapRegionPixels(SKBitmap bmp, int width, int height, SKRectI srcRegion)
        {
            using (var output = new SKBitmap(width, height))
            {
                bmp.ExtractSubset(output, srcRegion);
                output.LockPixels();
                //var data = SKData.CreateCopy(output.GetPixels(), (ulong)output.ByteCount);
                var img = SKImage.FromPixelCopy(output.Info, output.GetPixels());
                output.UnlockPixels();
                return img;
                //var img = SKImage.FromBitmap(output);
                //return SKImage.FromBitmap(SKBitmap.FromImage(img));
            }
        }

        // Benchmarking indicates using SKBitmap.ExtractSubset() -> SKImage -> SKBitmap -> SKImage (basically a copy) is faster
        public static SKImage CopyImageRegion(SKImage srcImage, int width, int height, SKRectI srcRegion)
        {
            using (var surface = SKSurface.Create(width: width, height: height, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul))
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
            using (var surface = SKSurface.Create(width: width, height: height, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul))
            using (var output = new SKBitmap(width, height))
            {
                var canvas = surface.Canvas;
                srcImage.ExtractSubset(output, srcRegion);
                canvas.DrawBitmap(output, new SKRect(0, 0, output.Width, output.Height), new SKRect(0, 0, width, height));
                return surface.Snapshot();
            }
        }
    }
}
