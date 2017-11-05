using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Image.Tiff.Test
{
    #region Mock Helpers
    public class MockHttpHandler : HttpMessageHandler
    {
        public virtual HttpResponseMessage Send(HttpRequestMessage request)
        {
            throw new NotImplementedException("Mock it");
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return await Task.FromResult(Send(request));
        }
    }
    #endregion
}
