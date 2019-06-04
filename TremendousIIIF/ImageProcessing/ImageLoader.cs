using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jpeg2000;
using Image.Common;
using Image.Tiff;
using TremendousIIIF.Common;
using System.Threading;
using LazyCache;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Buffers;
using OSGeo.GDAL;
using Extensions;
using GeoJSON.Net.Geometry;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra;
using OSGeo.OSR;
using GeoJSON.Net.Feature;

namespace TremendousIIIF.ImageProcessing
{
    public class ImageLoader
    {
        private readonly ILogger<ImageLoader> _log;
        private readonly IAppCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;

        public ImageLoader(ILogger<ImageLoader> log, IAppCache cache, IHttpClientFactory httpClientFactory)
        {
            _log = log;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// MagicBytes used to identify JPEG2000 or TIFF files (big or little endian) if unsuitable mimetype supplied
        /// </summary>
        private static readonly Dictionary<byte[], ImageFormat> MagicBytes = new Dictionary<byte[], ImageFormat> {
            { new byte[] { 0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A, 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x6A, 0x70, 0x32 }, ImageFormat.jp2 },
            // little endian
            { new byte[] { 0x49, 0x49, 0x2A, 0x00 }, ImageFormat.tif },
            // big endian
            { new byte [] { 0x4D, 0x4D, 0x00, 0x2A }, ImageFormat.tif }
        };

        public static int LongestBytes => MagicBytes.Keys.Max(k => k.Length);

        /// <summary>
        /// Extract region from source image
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="request">The <see cref="ImageRequest"/></param>
        /// <param name="allowUpscaling">Allow the output size to exceed the original dimensions of the image</param>
        /// <param name="quality">The <see cref="ImageQuality"/> settings for encoding</param>
        /// <returns></returns>
        public async Task<(ProcessState, SKImage)> ExtractRegion(Uri imageUri, ImageRequest request, bool allowUpscaling, Common.Configuration.ImageQuality quality, CancellationToken token = default)
        {
            switch (imageUri.Scheme)
            {
                case "http":
                case "https:":
                    (var format, var stream) = await LoadHttp(imageUri, token);
                    return await GetRegion(stream, format, imageUri, request, allowUpscaling, quality);
                case var _ when imageUri.IsFile:
                    using (var fs = new FileStream(imageUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, LongestBytes, useAsync: true))
                    {
                        var fformat = await _cache.GetOrAddAsync(imageUri.ToString(), () => GetSourceFormat(fs, token));
                        return await GetRegion(fs, fformat, imageUri, request, allowUpscaling, quality);
                    }
                default:
                    throw new IOException("Unsupported scheme");
            }
        }
        /// <summary>
        /// Get Metadata from the source image, for info.json requests
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="defaultTileWidth">The default tile width (pixels) if the source image is not natively tiled</param>
        /// <param name="requestId">The correlation ID to include on any subsequent HTTP requests</param>
        /// <returns></returns>
        public async Task<Metadata> GetMetadata(Uri imageUri, int defaultTileWidth, CancellationToken token = default)
        {
            switch (imageUri.Scheme)
            {
                case "http":
                case "https:":
                    (var format, var stream) = await LoadHttp(imageUri, token);
                    using (stream)
                        return await GetMetadata(stream, format, imageUri, defaultTileWidth);
                case var _ when imageUri.IsFile:
                    using (var fs = new FileStream(imageUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, LongestBytes, useAsync: true))
                    {
                        var fformat = await _cache.GetOrAddAsync(imageUri.ToString(), () => GetSourceFormat(fs, token));
                        return await GetMetadata(fs, fformat, imageUri, defaultTileWidth);
                    }
                default:
                    throw new IOException("Unsupported scheme");
            }
        }

        public async Task<Feature> GetGeoData(Uri imageUri, string geodataPath, CancellationToken token = default)
        {
            using (var fs = new FileStream(imageUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, LongestBytes, useAsync: true))
            {
                var format = await _cache.GetOrAddAsync(imageUri.ToString(), () => GetSourceFormat(fs, token));

                Gdal.AllRegister();
                Gdal.SetConfigOption("GDAL_DATA", geodataPath);

                switch (format)
                {
                    // For JP2, we support GEOJP2. Which is, incredibly, a 1x1 pixel GeoTiff embedded in a box with the GEOPJP2 UUID.
                    case ImageFormat.jp2:
                        (var width, var height, var data) = await J2KExpander.GetGeoData(_httpClientFactory.CreateClient(), _log, imageUri).ConfigureAwait(false);
                        Gdal.FileFromMemBuffer("/vsimem/in.tif", data);
                        if (null != data)
                        {
                            return TransformGeoData("/vsimem/in.tif", width, height);
                        }
                        throw new IOException("Unsupported source format");
                    case ImageFormat.tif:
                        return TransformGeoData(imageUri.LocalPath);
                    default:
                        throw new IOException("Unsupported source format");
                }
            }
        }

        public static Feature TransformGeoData(string fileName, int width = 0, int height = 0)
        {
            using (var src_ds = Gdal.Open(fileName, Access.GA_ReadOnly))
            using (var srs = new SpatialReference(src_ds.GetProjection()))
            using (var dst_proj = new SpatialReference(Osr.SRS_WKT_WGS84))
            using (var coords = Osr.CreateCoordinateTransformation(srs, dst_proj))
            {
                var transform = ArrayPool<double>.Shared.Rent(6);
                try
                {
                    src_ds.GetGeoTransform(transform);

                    var gpcCount = src_ds.GetGCPCount();
                    var gcp_proj = src_ds.GetGCPProjection();
                    var gpcs = src_ds.GetGCPs();

                    width = Math.Max(width, src_ds.RasterXSize);
                    height = Math.Max(height, src_ds.RasterYSize);

                    (var minX, var minY, var maxX, var maxY) = GetImageBorders(transform, src_ds.RasterXSize, src_ds.RasterYSize);
                    var points = new List<Point>();
                    foreach ((var lat, var lon) in GetCornerPoints(transform, width, height))
                    {
                        var ll = new double[3];
                        coords.TransformPoint(ll, lat, lon, 1);
                        points.Add(new Point(new Position(ll[0], ll[1])));
                    }


                    var mp = new MultiPoint(points);

                    var blah = new GeometryCollection(new[] { mp })
                    {

                    };

                    srs.ExportToProj4(out var src_proj_string);
                    var properties = new Dictionary<string, object>() { { "x:type", "CornerPoints" }, { "sourceProjection", src_proj_string } };
                    var feature = new GeoJSON.Net.Feature.Feature(blah, properties);


                    return feature;
                }
                finally
                {
                    ArrayPool<double>.Shared.Return(transform);
                }
            }
        }
        public static Matrix<double> FromGdal(double[] gt)
        {
            (double c, double a, double b, double f, double d, double e, _) = gt;

            return DenseMatrix.OfArray(new double[,] {
                {a, b, c},
                {d, e ,f},
                {0, 0, 1}
            });
        }

        public static IEnumerable<(double, double)> GetCornerPoints(double[] inlist, int ncol, int nrow)
        {
            var transform = FromGdal(inlist);

            yield return (transform[0, 2], transform[1, 2]); // upper left
            (var c1x, var c1y, _) = (transform * DenseVector.OfArray(new double[] { 0, nrow, 1 })).AsArray(); // lower left
            (var c2x, var c2y, _) = (transform * DenseVector.OfArray(new double[] { ncol, 0, 1 })).AsArray(); // upper right
            (var c3x, var c3y, _) = (transform * DenseVector.OfArray(new double[] { ncol, nrow, 1 })).AsArray(); // lower right


            yield return (c1x, c1y);
            yield return (c2x, c2y);
            yield return (c3x, c3y);
        }

        public static (double minX, double minY, double maxX, double maxY) GetImageBorders(double[] geoTransform, int rasterXSize, int rasterYSize)
        {
            double minX = geoTransform[0];
            double minY = geoTransform[3] - rasterYSize * geoTransform[1];
            double maxX = geoTransform[0] + rasterXSize * geoTransform[1];
            double maxY = geoTransform[3];

            return (minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Determine the image format of the source image, using mimetpye if available, otherwise using <see cref="MagicBytes"/>
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="requestId">The correlation ID to include on any subsequent HTTP requests</param>
        /// <returns></returns>
        public async Task<ImageFormat> GetSourceFormat(Stream fs, CancellationToken token)
        {
            var fileBytes = ArrayPool<byte>.Shared.Rent(LongestBytes);
            try
            {
                await fs.ReadAsync(fileBytes, 0, LongestBytes, token);
                var format = CompareMagicBytes(new ReadOnlySpan<byte>(fileBytes));
                fs.Seek(0, SeekOrigin.Begin);
                return format;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(fileBytes);
            }


            throw new IOException("Unsupported scheme");
        }

        private static ImageFormat CompareMagicBytes(in ReadOnlySpan<byte> fileBytes)
        {
            foreach (var mb in MagicBytes.OrderByDescending(v => v.Key.Length))
            {
                var subBytes = fileBytes.Slice(0, mb.Key.Length);
                var keySpan = new ReadOnlySpan<byte>(mb.Key);
                if (keySpan.SequenceEqual(subBytes))
                {
                    return mb.Value;
                }
            }
            throw new IOException("Unable to determine source format");
        }

        /// <summary>
        /// make a GET request and get on with it
        /// </summary>
        /// <param name="imageUri"></param>
        /// <param name="defaultTileWidth"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<(ImageFormat, Stream)> LoadHttp(Uri imageUri, CancellationToken token = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, imageUri))
            {
                var response = await _httpClientFactory.CreateClient("default").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                // Some failures we want to handle differently
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.NotFound:
                        throw new FileNotFoundException("Unable to load source image", imageUri.ToString());
                }

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogError("{@ImageUri} {@StatusCode} {@ReasonPhrase}", imageUri, response.StatusCode, response.ReasonPhrase);
                    throw new IOException("Unable to load source image");
                }
                var mimeType = string.Empty;

                //if (response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string> values))
                if (!string.IsNullOrEmpty(response.Content.Headers.ContentType?.MediaType))
                {
                    mimeType = response.Content.Headers.ContentType.MediaType;
                }

                ImageFormat imageFormat = ImageFormat.jp2;
                var resStream = await response.Content.ReadAsStreamAsync();

                if (mimeType == "text/plain" || mimeType == "application/octet-stream" || mimeType == string.Empty)
                {
                    // badly configured source server, read first x bytes to compare
                    // but the response from the HttpClient is a read-only, forward-only stream.
                    // so we need to copy them all back into a new stream :(
                    return await PeekMagicBytes(resStream, LongestBytes, token);
                }
                else
                {
                    imageFormat = GetFormatFromMimeType(mimeType);
                }
                return (imageFormat, resStream);
            }

        }
        private async ValueTask<Metadata> GetMetadata(Stream stream, ImageFormat imageFormat, Uri imageUri, int defaultTileWidth)
        {
            switch (imageFormat)
            {
                case ImageFormat.jp2:
                    return J2KExpander.GetMetadata(stream, _log, imageUri, defaultTileWidth);
                case ImageFormat.tif:
                    if (stream != null && stream.CanSeek)
                        return TiffExpander.GetMetadata(stream, _log, imageUri, defaultTileWidth);
                    // libtiff requires a seekable stream :(
                    // waste of memory
                    using (var ms = new MemoryStream())
                    {
                        if (null != stream)
                        {
                            await stream.CopyToAsync(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            stream.Dispose();
                        }
                        return TiffExpander.GetMetadata(ms, _log, imageUri, defaultTileWidth);
                    }
                default:
                    throw new IOException("Unsupported source format");
            }
        }

        private async ValueTask<(ProcessState, SKImage)> GetRegion(Stream stream, ImageFormat imageFormat, Uri imageUri, ImageRequest request, bool allowUpscaling, Common.Configuration.ImageQuality quality)
        {
            switch (imageFormat)
            {
                case ImageFormat.jp2:
                    return J2KExpander.ExpandRegion(stream, _log, imageUri, request, allowUpscaling, quality);
                case ImageFormat.tif:
                    if (stream != null && stream.CanSeek)
                        return TiffExpander.ExpandRegion(stream, _log, imageUri, request, allowUpscaling);
                    // libtiff requires a seekable stream :(
                    // waste of memory
                    using (var ms = new MemoryStream())
                    {
                        if (null != stream)
                        {
                            await stream.CopyToAsync(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                        }
                        return TiffExpander.ExpandRegion(ms, _log, imageUri, request, allowUpscaling);
                    }
                default:
                    throw new IOException("Unsupported source format");
            }
        }

        private async Task<(ImageFormat, Stream)> PeekMagicBytes(Stream stream, int maxBytes, CancellationToken token = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(maxBytes);

            try
            {
                var result = await stream.ReadAsync(buffer, 0, maxBytes, token);
                var format = CompareMagicBytes(new ReadOnlySpan<byte>(buffer));
                var ms = new MemoryStream();
                await ms.WriteAsync(buffer, 0, maxBytes, token);
                await stream.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                stream.Dispose();
                return (format, ms);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }


        /// <summary>
        /// Map mimetypes to <see cref="ImageFormat"/>
        /// </summary>
        /// <param name="mimeType">The mimetype to compare</param>
        /// <returns></returns>
        private static ImageFormat GetFormatFromMimeType(in string mimeType)
        {
            // still amazes me mimetype mapping isn't properly solved.
            switch (mimeType)
            {
                case "image/jp2":
                case "image/jpeg2000":
                case "image/jpeg2000-image":
                case "image/x-jpeg2000-image":
                    return ImageFormat.jp2;
                case "image/tif":
                case "image/tiff":
                case "image/x-tif":
                case "image/x-tiff":
                case "application/tif":
                case "application/x-tif":
                case "application/tiff":
                case "application/x-tiff":
                    return ImageFormat.tif;
                default:
                    throw new IOException("Unsupported source image format type");
            }
        }
    }

}
