using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TremendousIIIF.LibOwin;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace TremendousIIIF.Middleware
{
    public class MonitoringMiddleware
    {
        private Func<Task<bool>> healthCheck;
        private AppFunc next;


        private static readonly PathString monitorPath = new PathString("/_monitor");
        private static readonly PathString monitorShallowPath = new PathString("/_monitor/shallow");
        private static readonly PathString monitorDeepPath = new PathString("/_monitor/deep");


        public MonitoringMiddleware(AppFunc next, Func<Task<bool>> healthCheck)
        {
            this.next = next;
            this.healthCheck = healthCheck;
        }

        public Task InvokeAsync(IDictionary<string, object> env)
        {
            var context = new OwinContext(env);
            if (context.Request.Path.StartsWithSegments(monitorPath))
                return HandleMonitorEndpointAsync(context);
            else
                return next(env);
        }

        private Task HandleMonitorEndpointAsync(OwinContext context)
        {
            if (context.Request.Path.StartsWithSegments(monitorShallowPath))
                return ShallowEndpointAsync(context);
            else if (context.Request.Path.StartsWithSegments(monitorDeepPath))
                return DeepEndpointAsync(context);
            return Task.FromResult(0);
        }

        private async Task DeepEndpointAsync(OwinContext context)
        {
            if (await this.healthCheck())
                context.Response.StatusCode = 204;
            else
                context.Response.StatusCode = 503;
        }

        private Task ShallowEndpointAsync(OwinContext context)
        {
            context.Response.StatusCode = 204;
            return Task.FromResult(0);
        }
    }
}