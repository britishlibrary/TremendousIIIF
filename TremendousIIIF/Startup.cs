﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Serilog;
using Nancy.Owin;
using TremendousIIIF.Middleware;
using TremendousIIIF.Common.Configuration;

namespace TremendousIIIF
{
    public class Startup
    {
        private static HttpClient httpClient = new HttpClient(new HttpClientHandler { UseProxy = false, MaxConnectionsPerServer = 64  });
        private static ImageServer Conf = new ImageServer();
        private static ILogger Log;

        public void Configure(IApplicationBuilder app)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 1024;
            ConfigurationBinder.Bind(Configuration.GetSection("ImageServer"), Conf);

            app.UseOwin(buildFunc =>
            {
                buildFunc(next => RequestId.Middleware(next));
                buildFunc(next => SizeConstraints.Middleware(next));
                buildFunc(next => RequestLogging.Middleware(next, Log));
                buildFunc(next => PerformanceLogging.Middleware(next, Log));
                buildFunc(next => new MonitoringMiddleware(next, HealthCheckAsync).InvokeAsync);
                buildFunc.UseNancy(opt => opt.Bootstrapper = new Bootstrapper(Conf, Log, httpClient));
            });
        }
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            Log = ConfigureLogger();
        }

        public IConfiguration Configuration { get; }

        private ILogger ConfigureLogger()
        {
            return new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public async Task<bool> HealthCheckAsync()
        {
            var testImage = new Uri(new Uri(Conf.Location), Conf.HealthcheckIdentifier);
            try
            {
                var loader = new ImageProcessing.ImageLoader { HttpClient = httpClient, Log = Log };
                await loader.GetSourceFormat(testImage, "");
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Healthcheck failed");
                return false;
            }
        }
    }
}
