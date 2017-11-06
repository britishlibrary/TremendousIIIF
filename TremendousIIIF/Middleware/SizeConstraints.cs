using System;
using TremendousIIIF.LibOwin;

using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace TremendousIIIF.Middleware
{
    /// <summary>
    /// If rights proxy includes size constraints, these override the configured values
    /// Note it is up to the user to ensure these can not be spoofed
    /// </summary>
    public class SizeConstraints
    {
        public static AppFunc Middleware(AppFunc next)
        {
            return async env =>
            {
                var owinContext = new OwinContext(env);
                if (owinContext.Request.Headers.TryGetValue("X-maxWidth", out string[] maxWidth))
                {
                    if(Int32.TryParse(maxWidth[0],out int mw))
                    owinContext.Set("maxWidth", mw);
                }
                if (owinContext.Request.Headers.TryGetValue("X-maxHeight", out string[] maxHeight))
                {
                    if (Int32.TryParse(maxHeight[0], out int mw))
                        owinContext.Set("maxHeight", mw);
                }
                if (owinContext.Request.Headers.TryGetValue("X-maxArea", out string[] maxArea))
                {
                    if (Int32.TryParse(maxArea[0], out int mw))
                        owinContext.Set("maxArea", mw);
                }
                await next(env);
            };
        }
    }
}