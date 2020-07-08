using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading.Tasks;

namespace TremendousIIIF.Middleware
{
    /// <summary>
    /// If rights proxy includes size constraints, these override the configured values
    /// Note it is up to the user to ensure these can not be spoofed
    /// </summary>
    public class SizeConstraints
    {
        private readonly RequestDelegate _next;
        public SizeConstraints(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            if (httpContext.Request.Headers.TryGetValue("X-maxWidth", out StringValues maxWidth))
            {
                if (int.TryParse(maxWidth[0], out int mw))
                    httpContext.Items.Add("maxWidth", mw);
            }

            if (httpContext.Request.Headers.TryGetValue("X-maxHeight", out StringValues maxHeight))
            {
                if (int.TryParse(maxHeight[0], out int mh))
                    httpContext.Items.Add("maxHeight", mh);
            }

            if (httpContext.Request.Headers.TryGetValue("X-maxArea", out StringValues maxArea))
            {
                if (int.TryParse(maxArea[0], out int ma))
                    httpContext.Items.Add("maxArea", ma);
            }

            await _next(httpContext);
        }
    }
}