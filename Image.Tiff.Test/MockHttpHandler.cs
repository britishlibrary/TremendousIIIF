using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace Image.Tiff.Test
{
    [ExcludeFromCodeCoverage]
    public class MockHttpHandler : HttpMessageHandler
    {
        public virtual HttpResponseMessage Send(HttpRequestMessage request)
        {
            throw new NotImplementedException("Mock it");
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(Send(request));
        }
    }
}
