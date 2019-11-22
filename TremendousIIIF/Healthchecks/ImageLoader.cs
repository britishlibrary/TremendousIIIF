using LazyCache;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TremendousIIIF.Common.Configuration;

namespace TremendousIIIF.Healthchecks
{
    public class ImageLoader : IHealthCheck
    {
        public string Name => nameof(ImageLoader);
        private readonly ImageServer _conf;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ImageProcessing.ImageLoader> _logger;
        private readonly IAppCache _cache;

        public ImageLoader(ImageServer conf, IHttpClientFactory httpClientFactory, ILogger<ImageProcessing.ImageLoader> logger, IAppCache cache)
        {
            _conf = conf;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cache = cache;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var testImage = new Uri(new Uri(_conf.Location), _conf.HealthcheckIdentifier);
            try
            {
                var loader = new ImageProcessing.ImageLoader(_logger, _cache, _httpClientFactory);
                await loader.GetMetadata(testImage, _conf.DefaultTileWidth, cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Healthcheck failed");
                return HealthCheckResult.Unhealthy();
            }
        }
    }
}
