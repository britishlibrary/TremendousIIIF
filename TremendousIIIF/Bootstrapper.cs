using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Nancy.Owin;
using Serilog;
using System;
using Nancy.Responses.Negotiation;
using Microsoft.Extensions.Configuration;

//using TremendousIIIF.Handlers;
using System.Net.Http;
using TremendousIIIF.Common.Configuration;
using TremendousIIIF.Processors;

namespace TremendousIIIF
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework,
        // by overriding the various methods and properties.
        // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper
        private readonly ILogger log;
        private HttpClient httpClient;
        private ImageServer imageServer;

        public Bootstrapper(ImageServer imageServer, ILogger log, HttpClient httpClient)
        {
            this.log = log;
            this.httpClient = httpClient;
            this.imageServer = imageServer;
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            container.Register(log);
            container.Register(httpClient);
        }

        protected override void Dispose(bool disposing)
        {
            httpClient.Dispose();
            base.Dispose(disposing);
        }
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register(imageServer);
            container.Register<JsonSerializer, JsonLDSerializer>();
        }

        protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides(config =>
                {
                    //config.StatusCodeHandlers = new[] { typeof(StatusCodeHandler404), typeof(StatusCodeHandler500) };
                    //config.ResponseProcessors = new[] { typeof(JsonLdProcessor),typeof(JsonProcessor) };
                    config.ResponseProcessors = new[] { typeof(JsonLdProcessor) };
                });
            }
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            
            //CustomErrorHandler.Enable(pipelines, container.Resolve<IResponseNegotiator>(), log);
            //container.Register<IHttpClientFactory, HttpClientFactory>(new HttpClientFactory(requestId));
            //Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_ALL);
            //container.R
            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*");
            });
        }

    }

    public class JsonLDSerializer: JsonSerializer
    {
        public JsonLDSerializer()
        {
            NullValueHandling = NullValueHandling.Ignore;
            DefaultValueHandling = DefaultValueHandling.Ignore;
        }
    }
}