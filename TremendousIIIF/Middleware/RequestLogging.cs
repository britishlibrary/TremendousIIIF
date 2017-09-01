using TremendousIIIF.LibOwin;
using Serilog;

using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace TremendousIIIF.Middleware
{
    public class RequestLogging
    {
        public static AppFunc Middleware(AppFunc next, ILogger log)
        {
            return async env =>
            {
                var owinContext = new OwinContext(env);
                log.Information("Incoming Request: {@Method}, {@Path}, {@Header}", owinContext.Request.Method, owinContext.Request.Path, owinContext.Request.Headers);
                await next(env);
                log.Information("Outgoing Response: {@StatusCode}, {@Headers}, {@ContentType}, {@ContentLength}", owinContext.Response.StatusCode, owinContext.Response.Headers, owinContext.Response.ContentType, owinContext.Response.ContentLength);
            };
        }
    }
}