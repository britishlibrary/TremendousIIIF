using System;
using TremendousIIIF.LibOwin;
using Serilog.Context;

using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace TremendousIIIF.Middleware
{
    public class RequestId
    {
        public static AppFunc Middleware(AppFunc next)
        {
            return async env =>
            {
                var owinContext = new OwinContext(env);
                if (!owinContext.Request.Headers.TryGetValue("X-Request-ID", out string[] RequestId))
                {
                    RequestId = new string[] { Guid.NewGuid().ToString() };
                }
                owinContext.Set("RequestId", RequestId);
                using (LogContext.PushProperty("RequestId", RequestId[0]))
                {
                    await next(env);
                }
            };
        }
    }
}