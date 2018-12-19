using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SkiaSharp;
using Jpeg2000;
using Image.Common;
using Image.Tiff;
using System.Net.Http.Headers;
using TremendousIIIF.Common;
using Serilog;
using System.Threading;

namespace ImageProcessing
{
    public class ImageLoader
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public ImageLoader(HttpClient httpClient, ILogger log)
        {
            _httpClient = httpClient;
            _log = log;
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

        /// <summary>
        /// Extract region from source image
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="request">The <see cref="ImageRequest"/></param>
        /// <param name="allowUpscaling">Allow the output size to exceed the original dimensions of the image</param>
        /// <param name="quality">The <see cref="ImageQuality"/> settings for encoding</param>
        /// <returns></returns>
        public async Task<(ProcessState, SKImage)> ExtractRegion(Uri imageUri, ImageRequest request, bool allowUpscaling, TremendousIIIF.Common.Configuration.ImageQuality quality)
        {
            var sourceFormat = await GetSourceFormat(imageUri, request.RequestId);

            switch (sourceFormat)
            {
                case ImageFormat.jp2:
                    return await J2KExpander.ExpandRegion(_httpClient, _log, imageUri, request, allowUpscaling, quality).ConfigureAwait(false);
                case ImageFormat.tif:
                    return await TiffExpander.ExpandRegion(_httpClient, _log, imageUri, request, allowUpscaling);
                default:
                    throw new IOException("Unsupported source format");
            }
        }
        /// <summary>
        /// Get Metadata fromt he source image, for info.json requests
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="defaultTileWidth">The default tile width (pixels) if the source image is not natively tiled</param>
        /// <param name="requestId">The correlation ID to include on any subsequent HTTP requests</param>
        /// <returns></returns>
        public async Task<Metadata> GetMetadata(Uri imageUri, int defaultTileWidth, string requestId)
        {
            var sourceFormat = await Task.Run(() => GetSourceFormat(imageUri, requestId));
            switch (sourceFormat)
            {
                case ImageFormat.jp2:
                    return await J2KExpander.GetMetadata(_httpClient, _log, imageUri, defaultTileWidth, requestId).ConfigureAwait(false);
                case ImageFormat.tif:
                    return TiffExpander.GetMetadata(_httpClient, _log, imageUri, defaultTileWidth, requestId);
                default:
                    throw new IOException("Unsupported source format");
            }
        }

        /// <summary>
        /// Determine the image format of the source image, using mimetpye if available, otherwise using <see cref="MagicBytes"/>
        /// </summary>
        /// <param name="imageUri">The <see cref="Uri"/> of the source image</param>
        /// <param name="requestId">The correlation ID to include on any subsequent HTTP requests</param>
        /// <returns></returns>
        public async Task<ImageFormat> GetSourceFormat(Uri imageUri, string requestId, CancellationToken token = default)
        {
            var longest = MagicBytes.Keys.Max(k => k.Length);
            if (imageUri.Scheme == "http" || imageUri.Scheme == "https")
            {
                // issue a HEAD request, use supplied mimetype
                using (var headRequest = new HttpRequestMessage(HttpMethod.Head, imageUri))
                {
                    headRequest.Headers.Add("X-Request-ID", requestId);
                    using (var response = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, token))
                    {
                        // Some failures we want to handle differently
                        switch (response.StatusCode)
                        {
                            case System.Net.HttpStatusCode.NotFound:
                                throw new FileNotFoundException("Unable to load source image", imageUri.ToString());
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            _log.Error("{@ImageUri} {@StatusCode} {@ReasonPhrase}", imageUri, response.StatusCode, response.ReasonPhrase);
                            throw new IOException("Unable to load source image");
                        }

                        if (!response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string> values))
                        {
                            throw new FileLoadException("Unable to determine source format");
                        }
                        // remove any charset tags
                        var mime = values.First().Split(';').First();

                        if (mime.StartsWith("text/plain") || mime == "application/octet-stream")
                        {
                            // badly configured source server, issue a range request
                            using (var rangeRequest = new HttpRequestMessage(HttpMethod.Get, imageUri))
                            {
                                rangeRequest.Headers.Add("X-Request-Id", requestId);
                                rangeRequest.Headers.Range = new RangeHeaderValue(0, longest);
                                using (var byteResponse = await _httpClient.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead, token))
                                {
                                    byteResponse.EnsureSuccessStatusCode();
                                    var fileStream = await byteResponse.Content.ReadAsStreamAsync();
                                    var fileBytes = StreamToBytes(fileStream);
                                    return CompareMagicBytes(fileBytes);
                                }
                            }
                        }
                        var imageFormat = GetFormatFromMimeType(mime);

                        return imageFormat;
                    }
                }
            }
            else if (imageUri.IsFile)
            {
                using (var fs = new FileStream(imageUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, longest, true))
                using (var fr = new BinaryReader(fs))
                {
                    var fileBytes = fr.ReadBytes(longest);
                    return CompareMagicBytes(fileBytes);
                }
            }
            throw new IOException("Unsupported scheme");
        }

        private static byte[] StreamToBytes(Stream stream)
        {
            using (var sr = new BinaryReader(stream))
            {
                return sr.ReadBytes((int)stream.Length);
            }
        }


        /// <summary>
        /// Binary compare of <see cref="MagicBytes"/>
        /// </summary>
        /// <param name="fileBytes">The binary buffer to compare</param>
        /// <returns></returns>
        private static ImageFormat CompareMagicBytes(in byte[] fileBytes)
        {
            foreach (var mb in MagicBytes.OrderByDescending(v => v.Key.Length))
            {
                var subBytes = new ReadOnlySpan<byte>(fileBytes, 0, mb.Key.Length);
                var keySpan = new ReadOnlySpan<byte>(mb.Key);
                if (keySpan.SequenceEqual(subBytes))
                {
                    return mb.Value;
                }
            }
            throw new IOException("Unable to determine source format");
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
