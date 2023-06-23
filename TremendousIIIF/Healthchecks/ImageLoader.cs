using LazyCache;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace TremendousIIIF.Healthchecks
{
    public class ImageLoader : IHealthCheck
    {
        public string Name => nameof(ImageLoader);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly IAppCache _cache;
        private Uri ImageUri { get; set; }
        private int DefaultTileWidth { get; set; }

        public ImageLoader(IHttpClientFactory httpClientFactory, ILogger logger, IAppCache cache, Uri imageUri, int defaultTileWidth)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cache = cache;
            ImageUri = imageUri;
            DefaultTileWidth = defaultTileWidth;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var loader = new ImageProcessing.ImageLoader(_logger, _cache, _httpClientFactory);
                await loader.GetMetadata(ImageUri, DefaultTileWidth, cancellationToken);
                return HealthCheckResult.Healthy(data: new Dictionary<string, object>() { { "testImageUri", ImageUri } });
            }
            catch (Exception e) when (LogError(e))
            {
                return HealthCheckResult.Unhealthy(e.Message);
            }
        }

        bool LogError(Exception ex)
        {
            _logger.Error(ex, "A Healthcheck failed");
            return true;
        }
    }
}
