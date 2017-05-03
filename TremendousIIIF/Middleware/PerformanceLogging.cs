using Serilog;
using System.Diagnostics;
using TremendousIIIF.LibOwin;

namespace TremendousIIIF.Middleware
{
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;
    public class PerformanceLogging
    {
        public static AppFunc Middleware(AppFunc next, ILogger log)
        {
            return async env =>
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                await next(env);
                stopWatch.Stop();
                var owinContext = new OwinContext(env);
                log.Information("Request: {@Method} {@Path} executed in {RequestTime:0 0 0} ms", owinContext.Request.Method, owinContext.Request.Path, stopWatch.ElapsedMilliseconds);
                stopWatch.Stop();
            };
        }
    }
}