using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TremendousIIIF.Middleware;
using TremendousIIIF.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Linq;
using TremendousIIIF.Healthchecks;
using TremendousIIIF.Handlers;
using System.Net.Http;
using Polly.Extensions.Http;
using Polly;
using System.Collections.Generic;
using System.Diagnostics;

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
            services.AddCors(c=>c.AddPolicy("AllowAnyOrigin", options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
            //services.AddMvcCore(o=>o.OutputFormatters.Insert(0,new JsonLdFormatter()))
            //services.AddMvcCore(o => {
            //    var jsonOutputFormatter = o.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();
            //    jsonOutputFormatter?.SupportedMediaTypes.Insert(0,"application/ld+json");
            //})
            services.AddMvcCore()
                            .AddJsonFormatters(j => j.Formatting = Formatting.Indented)

            
                            .AddFormatterMappings(m => m.SetMediaTypeMappingForFormat("json", "application/ld+json"));

            

            services.AddSingleton(Configuration);
            var imageServerConf = new ImageServer();
            ConfigurationBinder.Bind(Configuration.GetSection("ImageServer"), imageServerConf);

            services.AddSingleton(imageServerConf);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<RequestIdMessageHandler>();

            services.AddHttpClient("default")
                .AddHttpMessageHandler<RequestIdMessageHandler>()
                .AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(DecorrelatedJitterBackoff(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10), 3)));

            services.AddSingleton<ImageProcessing.ImageLoader>();
            services.AddTransient<ImageProcessing.ImageProcessing>();
            services.AddHealthChecks().AddCheck<ImageLoader>("Image Loader");

            services.AddLazyCache();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAnyOrigin");

            app.UseMiddleware<SizeConstraints>();

            // shallow check
            app.UseHealthChecks("/_monitor", new HealthCheckOptions
            {
                Predicate = (check) => false
            });

            // deep check
            app.UseHealthChecks("/_monitor/deep", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    var result = JsonConvert.SerializeObject(
                        new
                        {
                            status = report.Status.ToString(),
                            errors = report.Entries.Select(e => new { key = e.Key, value = Enum.GetName(typeof(HealthStatus), e.Value.Status) })
                        });
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(result);
                }
            });


            app.UseMvc();
        }

        /// <summary>
        /// Generates sleep durations in an jittered manner, making sure to mitigate any correlations.
        /// For example: 117ms, 236ms, 141ms, 424ms, ...
        /// For background, see https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/.
        /// </summary>
        /// <param name="minDelay">The minimum duration value to use for the wait before each retry.</param>
        /// <param name="maxDelay">The maximum duration value to use for the wait before each retry.</param>
        /// <param name="retryCount">The maximum number of retries to use, in addition to the original call.</param>
        /// <param name="fastFirst">Whether the first retry will be immediate or not.</param>
        /// <param name="seed">An optional <see cref="Random"/> seed to use.
        /// If not specified, will use a shared instance with a random seed, per Microsoft recommendation for maximum randomness.</param>
        public static IEnumerable<TimeSpan> DecorrelatedJitterBackoff(TimeSpan minDelay, TimeSpan maxDelay, int retryCount, bool fastFirst = false, int? seed = null)
        {
            if (minDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "should be >= 0ms");
            if (maxDelay < minDelay) throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, $"should be >= {minDelay}");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");

            if (retryCount == 0)
                yield break;

            var random = new ConcurrentRandom(seed);

            int i = 0;
            if (fastFirst)
            {
                i++;
                yield return TimeSpan.Zero;
            }

            double ms = minDelay.TotalMilliseconds;
            for (; i < retryCount; i++)
            {
                // https://github.com/aws-samples/aws-arch-backoff-simulator/blob/master/src/backoff_simulator.py#L45
                // self.sleep = min(self.cap, random.uniform(self.base, self.sleep * 3))

                // Formula avoids hard clamping (which empirically results in a bad distribution)
                double ceiling = Math.Min(maxDelay.TotalMilliseconds, ms * 3);
                ms = random.Uniform(minDelay.TotalMilliseconds, ceiling);

                yield return TimeSpan.FromMilliseconds(ms);
            }
        }
        internal sealed class ConcurrentRandom
        {
            // Singleton approach is per MS best-practices.
            // https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netframework-4.7.2#the-systemrandom-class-and-thread-safety
            // https://stackoverflow.com/a/25448166/
            // Also note that in concurrency testing, using a 'new Random()' for every thread ended up
            // being highly correlated. On NetFx this is maybe due to the same seed somehow being used 
            // in each instance, but either way the singleton approach mitigated the problem.
            private static readonly Random s_random = new Random();
            private readonly Random _random;

            /// <summary>
            /// Creates an instance of the <see cref="ConcurrentRandom"/> class.
            /// </summary>
            /// <param name="seed">An optional <see cref="Random"/> seed to use.
            /// If not specified, will use a shared instance with a random seed, per Microsoft recommendation for maximum randomness.</param>
            public ConcurrentRandom(int? seed = null)
            {
                _random = seed == null
                    ? s_random // Do not use 'new Random()' here; in concurrent scenarios they could have the same seed
                    : new Random(seed.Value);
            }

            /// <summary>
            /// Returns a random floating-point number that is greater than or equal to 0.0,
            /// and less than 1.0.
            /// This method uses locks in order to avoid issues with concurrent access.
            /// </summary>
            public double NextDouble()
            {
                // It is safe to lock on _random since it's not exposed
                // to outside use so it cannot be contended.
                lock (_random)
                {
                    return _random.NextDouble();
                }
            }

            /// <summary>
            /// Returns a random floating-point number that is greater than or equal to <paramref name="a"/>,
            /// and less than <paramref name="b"/>.
            /// </summary>
            /// <param name="a">The minimum value.</param>
            /// <param name="b">The maximum value.</param>
            public double Uniform(double a, double b)
            {
                Debug.Assert(a <= b);

                if (a == b) return a;

                return a + (b - a) * NextDouble();
            }
        }


    }
}

