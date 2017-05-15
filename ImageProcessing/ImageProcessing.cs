using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;
using System.IO;
using Image.Common;
using TremendousIIIF.Common;
using System.Net.Http;
using Serilog;

namespace ImageProcessing
{
    public class ImageProcessing
    {
        public HttpClient HttpClient { get; set; }
        public ILogger Log { get; set; }

        private static Dictionary<ImageFormat, SKEncodedImageFormat> FormatLookup = new Dictionary<ImageFormat, SKEncodedImageFormat> {
            { ImageFormat.jpg, SKEncodedImageFormat.Jpeg },
            { ImageFormat.png, SKEncodedImageFormat.Png },
            //{ ImageFormat.gif, SKEncodedImageFormat.Gif },
            { ImageFormat.webp, SKEncodedImageFormat.Webp }
        };

        private static readonly float[] GreyMatrixData = new float[] {
                0.213f, 0.715f, 0.072f, 0, 0,
                0.213f, 0.715f, 0.072f, 0, 0,
                0.213f, 0.715f, 0.072f, 0, 0,
                0,     0,     0,        1, 0
            };

        // Region THEN Size THEN Rotation THEN Quality THEN Format
        public async Task<Stream> ProcessImage(Uri imageUri, ImageRequest request, TremendousIIIF.Common.Configuration.ImageQuality quality, bool allowSizeAboveFull)
        {
            if (!FormatLookup.TryGetValue(request.Format, out SKEncodedImageFormat formatType))
            {
                throw new ArgumentException("Unsupported format", "format");
            }
            var loader = new ImageLoader { HttpClient = HttpClient, Log = Log };
            (var state, var imageRegion) = await loader.ExtractRegion(imageUri, request, allowSizeAboveFull, quality);
            // There is a bug in SkiaSharp, https://github.com/mono/SkiaSharp/issues/209
            // until that's fixed, stick to this ugly call chaining

            //using (imageRegion)
            //using (var j = ResizeImage(imageRegion, request.Size, state))
            //using (var k = MirrorImage(j, request.Rotation))
            //using (var l = RotateImage(k, request.Rotation))
            //using (var m = AlterQuality(l, request.Quality))
            //{
            //    return EncodeImage(m, formatType, quality.GetOutputFormatQuality(request.Format));
            //}
            SKImage resized = null;
            if (request.Size.Mode == ImageSizeMode.Exact && ((state.Width >0 && state.Width != imageRegion.Width) || (state.Height > 0 && state.Height != imageRegion.Height)))
            {
                resized = ResizeImage(imageRegion, request.Size, state);
            }
            SKImage mirrored = null;
            if(request.Rotation.Mirror)
            {
                mirrored = MirrorImage(resized ?? imageRegion, request.Rotation);
            }
            SKImage rotate = null;
            if(request.Rotation.Degrees > 0 && request.Rotation.Degrees < 360)
            {
                rotate = RotateImage(mirrored ?? resized ?? imageRegion, request.Rotation);
            }
            SKImage qual = null;
            if(!(request.Quality == ImageQuality.@default || request.Quality == ImageQuality.color))
            {
                qual = AlterQuality(rotate??mirrored??resized??imageRegion, request.Quality);
            }

            var result = EncodeImage(qual ?? rotate ?? mirrored ?? resized ?? imageRegion, formatType, quality.GetOutputFormatQuality(request.Format));
            if (qual != null)
                qual.Dispose();
            if (rotate != null)
                rotate.Dispose();
            if (mirrored != null)
                mirrored.Dispose();
            if (resized != null)
                resized.Dispose();
            if (imageRegion != null)
                imageRegion.Dispose();
            return result;
        }

        private static SKImage ResizeImage(SKImage image, ImageSize size, ProcessState state)
        {
            if (size.Mode != ImageSizeMode.Exact)
            {
                return image;
            }
            // if width and height are already correct, also NOOP
            if (state.Width == image.Width && state.Height == image.Height)
            {
                return image;
            }
            // stretch output tile to requested dimension
            using (var surface = SKSurface.Create(width: state.Width, height: state.Height, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul))
            {
                var canvas = surface.Canvas;
                canvas.DrawImage(image, new SKRect(0, 0, state.Width, state.Height));
                canvas.Flush();
                return surface.Snapshot();
            }
        }

        public static Stream EncodeImage(SKImage image, SKEncodedImageFormat format, int q)
        {
            var output = new MemoryStream();
            using (var data = image.Encode(format, q))
            {
                data.SaveTo(output);
            }
            output.Seek(0, SeekOrigin.Begin);
            return output;
        }

        public static SKImage MirrorImage(SKImage image, ImageRotation rotation)
        {
            if (!rotation.Mirror)
            {
                return image;
            }

            using (var surface = SKSurface.Create(width: image.Width, height: image.Height, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul))
            {
                var canvas = surface.Canvas;
                canvas.Translate(image.Width, 0);
                canvas.Scale(-1, 1);
                canvas.DrawImage(image, 0, 0);
                canvas.Flush();
                return surface.Snapshot();
            }
        }

        public static SKImage RotateImage(SKImage image, ImageRotation rotation)
        {
            var degrees = rotation.Degrees;
            if (degrees == 0 || degrees == 360)
            {
                return image;
            }

            var angle = degrees % 360;
            if (angle > 180)
                angle -= 360;
            float sin = (float)Math.Abs(Math.Sin(angle * Math.PI / 180.0)); // this function takes radians
            float cos = (float)Math.Abs(Math.Cos(angle * Math.PI / 180.0)); // this one too
            float newImgWidth = sin * image.Height + cos * image.Width;
            float newImgHeight = sin * image.Width + cos * image.Height;
            float originX = 0f;
            float originY = 0f;

            if (angle > 0)
            {
                if (angle <= 90)
                    originX = sin * image.Height;
                else
                {
                    originX = newImgWidth;
                    originY = newImgHeight - sin * image.Width;
                }
            }
            else
            {
                if (angle >= -90)
                    originY = sin * image.Width;
                else
                {
                    originX = newImgWidth - sin * image.Height;
                    originY = newImgHeight;
                }
            }

            using (var surface = SKSurface.Create(width: Convert.ToInt32(newImgWidth), height: Convert.ToInt32(newImgHeight), colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Premul))
            {
                var canvas = surface.Canvas;

                canvas.Translate(originX, originY);
                canvas.RotateDegrees(angle, 0, 0);

                canvas.DrawImage(image, 0, 0);
                canvas.Flush();
                return surface.Snapshot();
            }
        }

        public static SKImage AlterQuality(SKImage image, ImageQuality quality)
        {
            switch (quality)
            {
                case ImageQuality.gray:
                    using (var greyMatrix = SKColorFilter.CreateColorMatrix(GreyMatrixData))
                    using (var greyFilter = SKImageFilter.CreateColorFilter(greyMatrix))
                    {
                        return ApplyFilter(image, greyFilter);
                    }

                case ImageQuality.bitonal:
                    using (var greyMatrix = SKColorFilter.CreateColorMatrix(GreyMatrixData))
                    using (var greyFilter = SKImageFilter.CreateColorFilter(greyMatrix))
                    using (var binaryMatrix = SKColorFilter.CreateColorMatrix(CreateThresholdMatrix(180)))
                    using (var binaryFilter = SKImageFilter.CreateColorFilter(binaryMatrix))
                    using (var bitonalFilter = SKImageFilter.CreateCompose(greyFilter, binaryFilter))
                    using (var bitonal = ApplyFilter(image, bitonalFilter))
                    using (var surface = SKSurface.Create(width: image.Width, height: image.Height, colorType: SKImageInfo.PlatformColorType, alphaType: SKAlphaType.Opaque))
                    {
                        var canvas = surface.Canvas;

                        canvas.DrawImage(bitonal, 0, 0);
                        canvas.Flush();
                        return surface.Snapshot();
                    }
                case ImageQuality.@default:
                case ImageQuality.color:
                default:
                    return image;
            }
        }

        private static SKImage ApplyFilter(SKImage image, SKImageFilter imageFilter)
        {
            var rect = new SKRectI(0, 0, image.Width, image.Height);
            var subset = new SKRectI();
            var offset = new SKPoint();
            return image.ApplyImageFilter(imageFilter, rect, rect, out subset, out offset);
        }

        private static float[] CreateThresholdMatrix(int threshold)
        {
            return new float[] {
                85f, 85f, 85f, 0f, -255f * threshold,
                85f, 85f, 85f, 0f, -255f * threshold,
                85f, 85f, 85f, 0f, -255f * threshold,
                0f, 0f, 0f, 1f, 0f
            };
        }

        public async Task<Metadata> GetImageInfo(Uri imageUri, int defaultTileWidth, string requestId)
        {
            var loader = new ImageLoader { HttpClient = HttpClient, Log = Log };
            return await loader.GetMetadata(imageUri, defaultTileWidth, requestId);
        }
    }
}
