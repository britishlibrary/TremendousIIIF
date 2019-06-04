using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TremendousIIIF.Handlers
{
    public class RequestIdMessageHandler : DelegatingHandler
    {

        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestIdMessageHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("X-Request-ID", _httpContextAccessor.HttpContext.TraceIdentifier);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
