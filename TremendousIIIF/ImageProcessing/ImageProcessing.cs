using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;
using System.IO;
using Image.Common;
using TremendousIIIF.Common;
using Conf = TremendousIIIF.Common.Configuration;
using RotationCoords = System.ValueTuple<float, float, float, int, int>;
using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace TremendousIIIF.ImageProcessing
{
    public class ImageProcessing
    {
        private readonly ILogger<ImageProcessing> _log;
        private readonly ImageLoader _loader;

        private static readonly Dictionary<ImageFormat, SKEncodedImageFormat> FormatLookup = new Dictionary<ImageFormat, SKEncodedImageFormat> {
            { ImageFormat.jpg, SKEncodedImageFormat.Jpeg },
            { ImageFormat.png, SKEncodedImageFormat.Png },
            { ImageFormat.webp, SKEncodedImageFormat.Webp }
        };

        private static readonly Dictionary<ImageQuality, SKColorFilter> ColourFilters = new Dictionary<ImageQuality, SKColorFilter> {
            { ImageQuality.gray, SKColorFilter.CreateHighContrast(true, SKHighContrastConfigInvertStyle.NoInvert, 0.1f)},
            { ImageQuality.bitonal, SKColorFilter.CreateHighContrast(true, SKHighContrastConfigInvertStyle.NoInvert, 1.0f)}
        };

        static ReadOnlySpan<byte> IDAT => new ReadOnlySpan<byte>(new byte[] { 0x49, 0x44, 0x41, 0x54 });

        public ImageProcessing(ILogger<ImageProcessing> log, ImageLoader loader)
        {
            _log = log;
            _loader = loader;
        }

        /// <summary>
        /// Process image pipeline
        /// <para>Region THEN Size THEN Rotation THEN Quality THEN Format</para>
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="request">The parsed and validated IIIF Image API request</param>
        /// <param name="quality">Image output encoding quality settings</param>
        /// <param name="allowSizeAboveFull">Allow output image dimensions to exceed that of the source image</param>
        /// <param name="pdfMetadata">Optional PDF metadata fields</param>
        /// <returns></returns>
        public async Task<Stream> ProcessImage(Uri imageUri, ImageRequest request, Conf.ImageQuality quality, bool allowSizeAboveFull, Conf.PdfMetadata pdfMetadata)
        {
            var encodingStrategy = GetEncodingStrategy(request.Format);
            if (encodingStrategy == EncodingStrategy.Unknown)
            {
                throw new ArgumentException("Unsupported format", "format");
            }

            var (state, imageRegion) = await _loader.ExtractRegion(imageUri, request, allowSizeAboveFull, quality);

            using (imageRegion)
            {
                var expectedWidth = state.OutputWidth;
                var expectedHeight = state.OutputHeight;
                var alphaType = request.Quality == ImageQuality.bitonal ? SKAlphaType.Opaque : SKAlphaType.Premul;
                // Some info for filter quality: https://groups.google.com/forum/#!topic/skia-discuss/Hpjma8785HA
                //  -  None - No filtering, just nearest-neighbor sampling.
                //  -  Low - Bilinear filtering.
                //  -  Medium - Adds mipmaps(so improves speed / quality on downscales, but still does bilinear for upscaling).
                //  -  High - Adds bicubic filtering when upscaling(continues to use mipmaps for downscale).
                // So, if you're using High, there is an abrupt algorithm change when the scale goes > 1, and the bicubic filter means many more samples for filtering.

                var paintQuality = state.ImageScale > 1.0 ? SKFilterQuality.High : SKFilterQuality.Medium;

                var (angle, originX, originY, newImgWidth, newImgHeight) = Rotate(expectedWidth, expectedHeight, request.Rotation.Degrees);

                using var surface = SKSurface.Create(new SKImageInfo(width: newImgWidth, height: newImgHeight, colorType: SKImageInfo.PlatformColorType, alphaType: alphaType, imageRegion.ColorSpace));
                using var canvas = surface.Canvas;
                using var region = new SKRegion();
                // If the rotation parameter includes mirroring ("!"), the mirroring is applied before the rotation.
                if (request.Rotation.Mirror)
                {
                    canvas.Translate(newImgWidth, 0);
                    canvas.Scale(-1, 1);
                }

                canvas.Translate(originX, originY);
                canvas.RotateDegrees(angle, 0, 0);
                // reset clip rects to rotated boundaries
                region.SetRect(new SKRectI(0 - (int)originX, 0 - (int)originY, newImgWidth, newImgHeight));
                canvas.ClipRegion(region);

                using (var paint = new SKPaint())
                {
                    paint.FilterQuality = paintQuality;
                    // if this is grey or bitonal, apply colour filter
                    ColourFilters.TryGetValue(request.Quality, out var cf);
                    paint.ColorFilter = cf;

                    canvas.DrawImage(imageRegion, new SKRect(0, 0, expectedWidth, expectedHeight), paint);
                }

                return Encode(surface,
                    expectedWidth,
                    expectedHeight,
                    encodingStrategy,
                    request.Format,
                    quality.GetOutputFormatQuality(request.Format),
                    pdfMetadata,
                    state.HorizontalResolution,
                    state.VerticalResolution);
            }
        }

        private static Stream Encode(in SKSurface surface, int width, int height, in EncodingStrategy encodingStrategy, in ImageFormat format, int q, in Conf.PdfMetadata pdfMetadata, ushort horizontalResolution, ushort verticalResolution)
        {
            switch (encodingStrategy)
            {
                case EncodingStrategy.Skia:
                    FormatLookup.TryGetValue(format, out SKEncodedImageFormat formatType);
                    return EncodeSkiaImage(surface, formatType, q, horizontalResolution, verticalResolution);
                case EncodingStrategy.PDF:
                    return EncodePdf(surface, width, height, q, pdfMetadata, horizontalResolution);
                case EncodingStrategy.JPEG2000:
                    return Jpeg2000.Compressor.Compress(surface.Snapshot());
                case EncodingStrategy.Tifflib:
                    return Image.Tiff.TiffEncoder.Encode(surface.Snapshot());
                case EncodingStrategy.Gif:
                    return GifEncoder.Encode(surface.Snapshot());
                default:
                    throw new ArgumentException("Unsupported format", "format");
            }
        }
        /// <summary>
        /// Rotate an image by arbitary degrees, to fit within supplied bounding box.
        /// </summary>
        /// <param name="width">Target width (pixels) of output image</param>
        /// <param name="height">Target height (pixels) of output image</param>
        /// <param name="degrees">arbitary precision degrees of rotation</param>
        /// <returns></returns>
        private static RotationCoords Rotate(int width, int height, float degrees)
        {
            // make it a NOOP for most common requests
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

        public static Stream EncodeSkiaImage(in SKSurface surface, in SKEncodedImageFormat format, int q, ushort horizontalResolution, ushort verticalResolution)
        {
            using var image = surface.Snapshot();
            var data = image.Encode(format, q);
            if (format == SKEncodedImageFormat.Jpeg)
            {
                unsafe {
                    SetJpgDpi(new Span<byte>((void*)data.Data, (int)data.Size), horizontalResolution, verticalResolution);
                }
            }
            if (format == SKEncodedImageFormat.Png)
            {
                var newdata = SetPngDpi(data.AsSpan(), (uint)Math.Round(horizontalResolution / 2.54 * 100), (uint)Math.Round(verticalResolution / 2.54 * 100));

                data.Dispose();
                unsafe
                {
                    fixed (byte* d = newdata)
                        data = SKData.Create(new IntPtr(d), newdata.Length);
                }

            }

            var ms = data.AsStream(false);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        /// <summary>
        /// Overwrite the DPI of a JPEG
        /// The Skia JPEG encoder sets a default DPI of 96x96, whith no way to change it. 
        /// </summary>
        /// <param name="jpgData"><see cref="IntPtr"/> Pointer to the data</param>
        /// <param name="horizontalResolution">Horizontal resolution, dots per inch</param>
        /// <param name="verticalResolution">Vertical resolution, dots per inch</param>
        /// 
        public static void SetJpgDpi(in Span<byte> jpgData, ushort horizontalResolution, ushort verticalResolution)
        {
            // The Skia JPEG encoder sets a default 96x96 DPI, so reset to original DPI

            // JPEG HEADER
            // SOI byte[2]
            // APP0 marker byte[2]
            // Length byte[2]
            // Identifier byte[5] = JIFF in ASCII followed by null byte
            // JIFF Version byte[2]
            // Density units byte[1]
            // XDensity byte[2]
            // YDensity byte[2]
            // 00 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17
            // FF D8 FF EO 00 10 4A 46 49 46 00 01 02 DD XX XX YY YY
            //...

            jpgData[13] = 0x01;
            // JPEG is big endian
            BinaryPrimitives.WriteUInt16BigEndian(jpgData.Slice(14, 2), horizontalResolution);
            BinaryPrimitives.WriteUInt16BigEndian(jpgData.Slice(16, 2), verticalResolution);

        }

        private static Span<byte> SetPngDpi(in ReadOnlySpan<byte> pngData, uint horizontalResolution, uint verticalResolution)
        {
            // PNG
            // The pHYs chunk specifies the intended pixel size or aspect ratio for display of the image. It contains:

            // Pixels per unit, X axis: 4 bytes(unsigned integer)
            // Pixels per unit, Y axis: 4 bytes(unsigned integer)
            // Unit specifier:          1 byte
            // PNG uses pixels per m

            // need to read each chunk until we find pHYs, then overwrite it
            // EXCEPT that the Skia PNG encoder doesn't set it at all.
            // pHYs must appear before first IDAT chunk

            Span<byte> physSpan = stackalloc byte[21];

            var idat_pos = pngData.IndexOf(IDAT) - 4; // beginning of chunk
            var preamble = pngData.Slice(0, idat_pos);
            var rest = pngData.Slice(idat_pos);


            BinaryPrimitives.WriteUInt32BigEndian(physSpan.Slice(0, 4), 9); // length of chunk data not including name or crc
            BinaryPrimitives.WriteUInt32BigEndian(physSpan.Slice(4, 4), 0x70485973); // pHYs
                                                                                     // chunk data
            BinaryPrimitives.WriteUInt32BigEndian(physSpan.Slice(8, 4), horizontalResolution);
            BinaryPrimitives.WriteUInt32BigEndian(physSpan.Slice(12, 4), verticalResolution);
            physSpan[16] = 0x01;
            // CRC32
            var crc = Crc32(physSpan.Slice(4, 13), 0, 13, 0);
            BinaryPrimitives.WriteUInt32BigEndian(physSpan.Slice(17, 4), crc);

            // can we just ref 
            var outputSpan = new Span<byte>(new byte[preamble.Length + physSpan.Length + rest.Length]);
            preamble.CopyTo(outputSpan);
            var idx = preamble.Length;
            physSpan.CopyTo(outputSpan.Slice(idx));
            idx += physSpan.Length;
            rest.CopyTo(outputSpan.Slice(idx));

            return outputSpan;
        }

        static uint[] crcTable;

        // How is there not a framework method for this somewhere?!
        private static uint Crc32(in ReadOnlySpan<byte> stream, int offset, int length, uint crc)
        {
            uint c;
            if (crcTable == null)
            {
                crcTable = new uint[256];
                for (uint n = 0; n <= 255; n++)
                {
                    c = n;
                    for (var k = 0; k <= 7; k++)
                    {
                        if ((c & 1) == 1)
                            c = 0xEDB88320 ^ ((c >> 1) & 0x7FFFFFFF);
                        else
                            c = ((c >> 1) & 0x7FFFFFFF);
                    }
                    crcTable[n] = c;
                }
            }
            c = crc ^ 0xffffffff;
            var endOffset = offset + length;
            for (var i = offset; i < endOffset; i++)
            {
                c = crcTable[(c ^ stream[i]) & 255] ^ ((c >> 8) & 0xFFFFFF);
            }
            return c ^ 0xffffffff;
        }

        /// <summary>
        /// Encode output image as PDF
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="width">Requested output width (pixels), because you can't get a surface's dimensions once created(?)</param>
        /// <param name="height">Requested output height (pixels)</param>
        /// <param name="q">Image quality (percentage)</param>
        /// <param name="pdfMetadata">Optional metadata to include in the PDF</param>
        /// <param name="dpi">The pixels per inch resolution that images will be rasterised at in the PDF</param>
        /// <returns></returns>
        public static Stream EncodePdf(in SKSurface surface, int width, int height, int q, in Conf.PdfMetadata pdfMetadata, ushort dpi)
        {
            var output = new MemoryStream();

            var metadata = new SKDocumentPdfMetadata()
            {
                Creation = DateTime.Now,
                EncodingQuality = q,
                RasterDpi = dpi
            };

            if (null != pdfMetadata)
            {
                metadata.Author = pdfMetadata.Author;
                metadata.Producer = pdfMetadata.Author;
            }

            using (var writer = SKDocument.CreatePdf(output, metadata))
            using (var paint = new SKPaint())
            {
                using var canvas = writer.BeginPage(width, height);
                paint.FilterQuality = SKFilterQuality.High;
                canvas.DrawSurface(surface, new SKPoint(0, 0), paint);
                writer.EndPage();
            }
            output.Seek(0, SeekOrigin.Begin);
            return output;
        }

        /// <summary>
        /// Load source image and extract enough Metadata to create an info.json
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="defaultTileWidth">The default tile width (pixels) to use for the source image, if source is not natively tiled</param>
        /// <param name="requestId">The <code>X-RequestId</code> value to include on any subsequent HTTP calls</param>
        /// <returns></returns>
        public Task<Metadata> GetImageInfo(Uri imageUri, int defaultTileWidth, CancellationToken token = default)
        {
            return _loader.GetMetadata(imageUri, defaultTileWidth, token);
        }

        public Task<GeoJSON.Net.Feature.Feature> GetImageGeoData(Uri imageUri, string geodataPath, CancellationToken token = default)
        {
            return _loader.GetGeoData(imageUri, geodataPath, token);
        }

        /// <summary>
        /// Determines output encoding strategy based on supplied <paramref name="format"/>
        /// </summary>
        /// <param name="format">Requested output format type</param>
        /// <returns><see cref="EncodingStrategy"/></returns>
        private static EncodingStrategy GetEncodingStrategy(in ImageFormat format)
        {
            return format switch
            {
                _ when FormatLookup.ContainsKey(format) => EncodingStrategy.Skia,
                ImageFormat.pdf => EncodingStrategy.PDF,
                ImageFormat.jp2 => EncodingStrategy.JPEG2000,
                ImageFormat.tif => EncodingStrategy.Tifflib,
                ImageFormat.gif => EncodingStrategy.Gif,
                _ => EncodingStrategy.Unknown,
            };
        }

        private enum EncodingStrategy
        {
            Unknown,
            Skia,
            PDF,
            JPEG2000,
            Tifflib,
            Gif
        }
    }
}
