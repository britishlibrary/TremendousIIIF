using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;
using System.IO;
using Image.Common;
using TremendousIIIF.Common;
using System.Net.Http;
using Serilog;
using RotationCoords = System.ValueTuple<float, float, float, int, int>;

namespace ImageProcessing
{
    enum EncodingStrategy
    {
        Unknown,
        Skia,
        PDF,
        Kakadu,
        Tifflib
    }
    public class ImageProcessing
    {
        public HttpClient HttpClient { get; set; }
        public ILogger Log { get; set; }

        private static Dictionary<ImageFormat, SKEncodedImageFormat> FormatLookup = new Dictionary<ImageFormat, SKEncodedImageFormat> {
            { ImageFormat.jpg, SKEncodedImageFormat.Jpeg },
            { ImageFormat.png, SKEncodedImageFormat.Png },
            { ImageFormat.webp, SKEncodedImageFormat.Webp },
            // GIF appears to be unsupported on Windows in Skia currently
            // { ImageFormat.gif, SKEncodedImageFormat.Gif }
        };

        private EncodingStrategy GetEncodingStrategy(ImageFormat format)
        {
            if (FormatLookup.ContainsKey(format))
            {
                return EncodingStrategy.Skia;
            }
            if(ImageFormat.pdf == format)
            {
                return EncodingStrategy.PDF;
            }
            if(ImageFormat.jp2 == format)
            {
                return EncodingStrategy.Kakadu;
            }
            if(ImageFormat.tif == format)
            {
                return EncodingStrategy.Tifflib;
            }

            return EncodingStrategy.Unknown;

        }

        private static readonly float[] GreyMatrixData = new float[] {
                0.213f, 0.715f, 0.072f, 0, 0,
                0.213f, 0.715f, 0.072f, 0, 0,
                0.213f, 0.715f, 0.072f, 0, 0,
                0,     0,     0,        1, 0
            };

        // Region THEN Size THEN Rotation THEN Quality THEN Format
        public async Task<Stream> ProcessImage(Uri imageUri, ImageRequest request, TremendousIIIF.Common.Configuration.ImageQuality quality, bool allowSizeAboveFull)
        {
            var encodingStrategy = GetEncodingStrategy(request.Format);
            if(encodingStrategy == EncodingStrategy.Unknown)
            {
                throw new ArgumentException("Unsupported format", "format");
            }

            var loader = new ImageLoader { HttpClient = HttpClient, Log = Log };
            (var state, var imageRegion) = await loader.ExtractRegion(imageUri, request, allowSizeAboveFull, quality);

            using (imageRegion)
            {
                var expectedWidth = request.Size.Mode == ImageSizeMode.Exact ? state.Width : state.TileWidth;
                var expectedHeight = request.Size.Mode == ImageSizeMode.Exact ? state.Height : state.TileHeight;
                var alphaType = request.Quality == ImageQuality.bitonal ? SKAlphaType.Opaque : SKAlphaType.Premul;

                (float angle, float originX, float originY, int newImgWidth, int newImgHeight) = Rotate(expectedWidth, expectedHeight, request.Rotation.Degrees);

                using (var surface = SKSurface.Create(width: newImgWidth, height: newImgHeight, colorType: SKImageInfo.PlatformColorType, alphaType: alphaType))
                using (var canvas = surface.Canvas)
                {
                    if (request.Rotation.Mirror)
                    {
                        canvas.Translate(newImgWidth, 0);
                        canvas.Scale(-1, 1);
                    }

                    canvas.Translate(originX, originY);
                    canvas.RotateDegrees(angle, 0, 0);

                    // reset clip rects to rotated boundaries
                    var region = new SKRegion();
                    var tr = request.Rotation.Mirror ? 1 : -1;
                    region.SetRect(new SKRectI(0 - (int)originX, 0 - (int)originY, newImgWidth, newImgHeight));
                    canvas.ClipRegion(region);

                    // quality
                    if (request.Quality == ImageQuality.gray || request.Quality == ImageQuality.bitonal)
                    {
                        var contrast = request.Quality == ImageQuality.gray ? 0.1f : 1f;
                        using (var cf = SKColorFilter.CreateHighContrast(true, SKHighContrastConfigInvertStyle.NoInvert, contrast))
                        using (var paint = new SKPaint())
                        {
                            paint.FilterQuality = SKFilterQuality.High;
                            paint.ColorFilter = cf;

                            canvas.DrawImage(imageRegion, new SKRect(0, 0, expectedWidth, expectedHeight), paint);
                        }
                    }
                    else
                    {
                        using (var paint = new SKPaint())
                        {
                            paint.FilterQuality = SKFilterQuality.High;
                            canvas.DrawImage(imageRegion, new SKRect(0, 0, expectedWidth, expectedHeight), paint);
                        }
                    }

                    return Encode(surface, expectedWidth, expectedHeight, encodingStrategy, request.Format, quality.GetOutputFormatQuality(request.Format));
                    
                }
            }
        }

        private static Stream Encode(SKSurface surface, int width, int height, EncodingStrategy encodingStrategy, ImageFormat format, int q)
        {
            switch (encodingStrategy)
            {
                case EncodingStrategy.Skia:
                    FormatLookup.TryGetValue(format, out SKEncodedImageFormat formatType);
                    return EncodeImage(surface.Snapshot(), formatType, q);
                case EncodingStrategy.PDF:
                    return EncodePdf(surface, width, height, q);
                case EncodingStrategy.Kakadu:
                    return Jpeg2000.Compressor.Compress(surface.Snapshot());
                case EncodingStrategy.Tifflib:
                    return Image.Tiff.TiffEncoder.Encode(surface.Snapshot());
                default:
                    throw new ArgumentException("Unsupported format");
            }
        }

        private static RotationCoords Rotate(int width, int height, float degrees)
        {
            if (degrees == 0 || degrees == 360)
            {
                return (0, 0, 0, width, height);
            }

            var angle = degrees % 360;
            if (angle > 180)
                angle -= 360;
            float sin = (float)Math.Abs(Math.Sin(angle * Math.PI / 180.0)); // this function takes radians
            float cos = (float)Math.Abs(Math.Cos(angle * Math.PI / 180.0)); // this one too
            float newImgWidth = sin * height + cos * width;
            float newImgHeight = sin * width + cos * height;
            float originX = 0f;
            float originY = 0f;

            if (angle > 0)
            {
                if (angle <= 90)
                    originX = sin * height;
                else
                {
                    originX = newImgWidth;
                    originY = newImgHeight - sin * width;
                }
            }
            else
            {
                if (angle >= -90)
                    originY = sin * width;
                else
                {
                    originX = newImgWidth - sin * height;
                    originY = newImgHeight;
                }
            }

            return (angle, originX, originY, (int)newImgWidth, (int)newImgHeight);
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

        public static Stream EncodePdf(SKSurface surface, int width, int height, int q)
        {
            // have to encode to JPEG then paint the encoded bytes, otherwise you get full JP2 quality
            var output = new MemoryStream();
            using (var skstream = new SKManagedWStream(output))
            using (var writer = SKDocument.CreatePdf(skstream))
            using (var snapshot = surface.Snapshot())
            using (var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, q))
            using (var image = SKImage.FromEncodedData(data))
            {
                using (var canvas = writer.BeginPage(width, height))
                {
                    canvas.DrawImage(image, 0, 0);
                    writer.EndPage();
                }
                writer.Close();
            }
            output.Seek(0, SeekOrigin.Begin);
            return output;
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
                    {
                        return ApplyFilter(image, bitonalFilter);
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
