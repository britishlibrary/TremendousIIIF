using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TremendousIIIF.Middleware;
using TremendousIIIF.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using TremendousIIIF.Healthchecks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Microsoft.Extensions.Hosting;
using HealthChecks.UI.Client;
using Microsoft.OpenApi.Models;

namespace TremendousIIIF
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(c => c.AddPolicy("AllowAnyOrigin", options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
            services.AddHeaderPropagation(o => o.Headers.Add("X-Request-ID", c => new Guid().ToString()));

            services.AddControllers()
                // until the new System.Text.Json allows ordering
                .AddNewtonsoftJson()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.WriteIndented = true;
                    o.JsonSerializerOptions.IgnoreNullValues = true;
                    o.JsonSerializerOptions.PropertyNamingPolicy = null;
                })
                .AddMvcOptions(o => o.RespectBrowserAcceptHeader = true)
                .AddFormatterMappings(m => m.SetMediaTypeMappingForFormat("json", "application/ld+json"));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TremendousIIIF", Version = "v1" });
            });

            services.AddSingleton(Configuration);
            var imageServerConf = new ImageServer();
            ConfigurationBinder.Bind(Configuration.GetSection("ImageServer"), imageServerConf);

            services.AddSingleton(imageServerConf);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddHttpClient("default")
                .AddHeaderPropagation()
                .AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(10), 3))); ;

            services.AddSingleton<ImageProcessing.ImageLoader>();
            services.AddTransient<ImageProcessing.ImageProcessing>();

            var testImage = new Uri(new Uri(imageServerConf.Location), imageServerConf.HealthcheckIdentifier);
            services.AddHealthChecks()
                    .AddTypeActivatedCheck<ImageLoader>("Image Loader", new object[] { testImage, imageServerConf.DefaultTileWidth });

            services.AddLazyCache();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("./v1/swagger.json", "TremendousIIIF");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors("AllowAnyOrigin");
            app.UseHeaderPropagation();

            app.UseMiddleware<SizeConstraints>();

            // shallow check
            app.UseHealthChecks("/_monitor", new HealthCheckOptions
            {
                Predicate = (check) => false
            });

            // deep check
            app.UseHealthChecks("/_monitor/deep", new HealthCheckOptions
            {
                Predicate = _ => true
            });

            // healthcheck UI
            app.UseHealthChecks("/_monitor/detail", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
}

