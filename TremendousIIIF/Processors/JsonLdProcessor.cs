using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Responses;
using Nancy.Responses.Negotiation;

namespace TremendousIIIF.Processors
{
    
    /// <summary>
    /// Processes the model for JSON-LD media types and extension.
    /// </summary>
    public class JsonLdProcessor : IResponseProcessor
    {
        private readonly ISerializer serializer;

        private static readonly IEnumerable<Tuple<string, MediaRange>> extensionMappings =
            new[] {
               new Tuple<string, MediaRange>("json", new MediaRange("application/json")),
               new Tuple<string, MediaRange>("json-ld",new MediaRange("application/ld+json"))
             };

        public void Process(object p, object dynamic, object model, object nancyContext, object context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLdProcessor"/> class,
        /// with the provided <paramref name="serializers"/>.
        /// </summary>
        /// <param name="serializers">The serializes that the processor will use to process the request.</param>
        public JsonLdProcessor(IEnumerable<ISerializer> serializers)
        {
            this.serializer = serializers.FirstOrDefault(x => x.CanSerialize("application/json"));
        }

        /// <summary>
        /// Gets a set of mappings that map a given extension (such as .json)
        /// to a media range that can be sent to the client in a vary header.
        /// </summary>
        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings
        {
            get { return extensionMappings; }
        }

        /// <summary>
        /// Determines whether the processor can handle a given content type and model
        /// </summary>
        /// <param name="requestedMediaRange">Content type requested by the client</param>
        /// <param name="model">The model for the given media range</param>
        /// <param name="context">The nancy context</param>
        /// <returns>A ProcessorMatch result that determines the priority of the processor</returns>
        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            if (IsExactJsonContentType(requestedMediaRange))
            {
                return new ProcessorMatch
                {
                    ModelResult = MatchResult.ExactMatch,
                    RequestedContentTypeResult = MatchResult.ExactMatch
                };
            }

            if (IsWildcardJsonContentType(requestedMediaRange))
            {
                return new ProcessorMatch
                {
                    ModelResult = MatchResult.NonExactMatch,
                    RequestedContentTypeResult = MatchResult.NonExactMatch
                };
            }

            return new ProcessorMatch
            {
                ModelResult = MatchResult.DontCare,
                RequestedContentTypeResult = MatchResult.NoMatch
            };
        }

        /// <summary>
        /// Process the response
        /// </summary>
        /// <param name="requestedMediaRange">Content type requested by the client</param>
        /// <param name="model">The model for the given media range</param>
        /// <param name="context">The nancy context</param>
        /// <returns>A response</returns>
        public Response Process(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            if (!requestedMediaRange.Matches("application/ld+json"))
                return new JsonResponse(model, this.serializer, context.Environment);
            return new JsonLdResponse(model, this.serializer, context.Environment);
        }

        private static bool IsExactJsonContentType(MediaRange requestedContentType)
        {
            if (requestedContentType.Type.IsWildcard && requestedContentType.Subtype.IsWildcard)
            {
                return true;
            }

            return requestedContentType.Matches("application/json") || requestedContentType.Matches("text/json") || requestedContentType.Matches("application/ld+json");
        }

        private static bool IsWildcardJsonContentType(MediaRange requestedContentType)
        {
            if (!requestedContentType.Type.IsWildcard && !string.Equals("application", requestedContentType.Type, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (requestedContentType.Subtype.IsWildcard)
            {
                return true;
            }

            var subtypeString = requestedContentType.Subtype.ToString();

            return (subtypeString.StartsWith("vnd", StringComparison.OrdinalIgnoreCase) &&
                    subtypeString.EndsWith("+json", StringComparison.OrdinalIgnoreCase));
        }
    }
}
