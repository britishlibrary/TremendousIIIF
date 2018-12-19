using Nancy;
using Nancy.Configuration;
using Nancy.Responses;

namespace TremendousIIIF.Processors
{
    public class JsonLdResponse : JsonResponse
    {
        public JsonLdResponse(object model, ISerializer serializer, INancyEnvironment environment) : base(model, serializer, environment)
        {
            this.ContentType = "application/ld+json; profile=\"http://iiif.io/api/image/3/context.json\"";
        }

        
    }
}