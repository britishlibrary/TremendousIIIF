using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog;
using TremendousIIIF.Common.Configuration;
using TremendousIIIF.ImageProcessing;
using TremendousIIIF.Middleware;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(c => c.AddPolicy("AllowAnyOrigin", options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddHeaderPropagation(o => o.Headers.Add("X-Request-ID", c => new Guid().ToString()));
builder.Services.AddControllers()
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TremendousIIIF", Version = "v1" });
    var filePath = Path.Combine(AppContext.BaseDirectory, "TremendousIIIF.xml");
    c.IncludeXmlComments(filePath);
});

builder.Services.AddSingleton(config);
var imageServerConf = new ImageServer();
ConfigurationBinder.Bind(config.GetSection("ImageServer"), imageServerConf);
imageServerConf.LoginDataString = config["ImageServer:LoginService"];

builder.Services.AddSingleton(imageServerConf);
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddHttpClient("default")
    .AddHeaderPropagation()
    .AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(10), 3))); ;


builder.Services.AddSingleton<ImageLoader>();
builder.Services.AddTransient<ImageProcessing>();

builder.Services.AddSingleton(Log.Logger);

builder.Host.UseSerilog();

var testImage = new Uri(new Uri(imageServerConf.Location), imageServerConf.HealthcheckIdentifier);
//TODO:HealthChecks
//builder.Services.AddHealthChecks().AddTypeActivatedCheck<ImageLoader>("Image Loader", new object[] { testImage, imageServerConf.DefaultTileWidth });

builder.Services.AddLazyCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        string swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
        c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v1/swagger.json", "TremendousIIIF");
    });
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors("AllowAnyOrigin");
app.UseHeaderPropagation();

app.UseMiddleware<SizeConstraints>();

// shallow check
//app.UseHealthChecks("/_monitor", new HealthCheckOptions
//{
//    Predicate = (check) => false
//});

//// deep check
//app.UseHealthChecks("/_monitor/deep", new HealthCheckOptions
//{
//    Predicate = _ => true
//});

//// healthcheck UI
//app.UseHealthChecks("/_monitor/detail", new HealthCheckOptions
//{
//    Predicate = _ => true,
//    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
//});

app.UseHttpsRedirection();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
