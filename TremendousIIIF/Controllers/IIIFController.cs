using Image.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TremendousIIIF.Common;
using TremendousIIIF.Common.Configuration;
using TremendousIIIF.Types;
using TremendousIIIF.Validation;

namespace TremendousIIIF.Controllers
{
    [ApiController]
    public class IIIFController : ControllerBase
    {
        readonly ImageServer Conf;
        readonly ImageProcessing.ImageProcessing Processor;
        readonly ILogger<IIIFController> _log;
        private readonly LinkGenerator _generator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        readonly static List<string> _validRightsDomains = new List<string> { "creativecommons.org", "rightsstatements.org" };

        public IIIFController(ILogger<IIIFController> log, ImageServer conf, ImageProcessing.ImageProcessing processor, LinkGenerator generator, IHttpContextAccessor httpContextAccessor)
        {
            Conf = conf;
            Processor = processor;
            _log = log;
            _generator = generator;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// The method that gets all the relevant info for an image and gives us the parameters and boundaries required to perform the ImageRequest (I presume)
        /// </summary>
        /// <param name="id">the id/name of the image appended to "Location" in the appsettings.json to form a Uri</param>
        /// <param name="Accept">The api version can be passed as a custom mediaType property in the header to the API from which defines the api Version and resolves to one of two different "ImageInfo" formats for the returned json info. Else the default defined in config is used.</param>
        /// <param name="manifestId">The IIIF manifest id of the image I presume</param>
        /// <param name="licence">FromHeader(Name = "X-LicenceUri") from the code comments: The value of this property MUST be a string drawn from the set of Creative Commons license URIs, the RightsStatements.org rights statement URIs, or those added via the Registry of Known Extensions mechanism we assume the upstream system is setting the correct URI and just validate the domain</param>
        /// <returns>application/json</returns>
        [HttpGet("/{id}/info.json", Name = "info.json")]
        [Produces("application/json", "application/ld+json;profile=\"http://iiif.io/api/image/2/context.json\"", "application/ld+json;profile=\"http://iiif.io/api/image/3/context.json\"")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<IImageInfo>> Info(string id, [FromHeader] string Accept, [FromHeader(Name = "X-PartOf-Manifest")] string manifestId, [FromHeader(Name = "X-LicenceUri")] string licence)
        {
            var cancellationToken = HttpContext.RequestAborted;
            var apiVersion = ParseAccept(Accept, Conf.DefaultAPIVersion);
            try
            {
                var imageUri = new Uri(new Uri(Conf.Location), id);
                (var maxWidth, var maxHeight, var maxArea) = GetSizeConstraints(Conf);
                var metadata = await Processor.GetImageInfo(imageUri, Conf.DefaultTileWidth, cancellationToken);

                var host = new UriBuilder(Request.Host.ToString());
                var requestUrl = _generator.GetUriByName(_httpContextAccessor.HttpContext, "base", new { id });

                Response.Headers
                        .Add("Link",
                            new[] { $"<{apiVersion.GetAttribute<ProfileUriAttribute>().ProfileUri}>;rel=\"profile\""
                        });

                if (!string.IsNullOrEmpty(manifestId))
                {
                    manifestId = string.Format(Conf.ManifestUriFormat, manifestId);
                }
                return Ok(MakeInfo(apiVersion, metadata, Conf, maxWidth, maxHeight, maxArea, id, requestUrl, manifestId, licence));
            }
            catch (FileNotFoundException e)
            {
                _log.LogError("Unable to load source image @{FileName}", e.FileName);
                return NotFound(new { error = "Unable to load source image" });
            }
            catch (Exception e) when (e is NotImplementedException || e is ArgumentException)
            {
                _log.LogError(e, "Error parsing argument");
                var problem = new ValidationProblemDetails
                {
                    Detail = e.Message
                };

                return BadRequest(problem);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unexpected exception");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static object MakeInfo(ApiVersion apiVersion, Metadata metadata, ImageServer conf, int maxWidth, int maxHeight, int maxArea, string id, string requestUri, string manifestId, string licence)
        {
            var idUri = conf.BaseUri == null ?
                requestUri :
                new Uri(conf.BaseUri, id).ToString();

            if (Uri.TryCreate(licence, UriKind.Absolute, out var licenceUri))
            {
                // The value of this property MUST be a string drawn from the set of Creative Commons license URIs, 
                // the RightsStatements.org rights statement URIs, or those added via the Registry of Known Extensions mechanism
                // we assume the upstream system is setting the correct URI and just validate the domain
                if (!_validRightsDomains.Contains(licenceUri.Host))
                {
                    licenceUri = null;
                }
            }

            return apiVersion switch
            {
                ApiVersion.v3_0 => new Types.v3_0.ImageInfo(idUri, metadata, conf, maxWidth, maxHeight, maxArea, conf.EnableGeoService, manifestId, licenceUri),
                _ => new Types.v2_1.ImageInfo(metadata, conf, maxWidth, maxHeight, maxArea, conf.EnableGeoService, string.Format(conf.GeoDataBaseUri, id))
                {
                    ID = idUri,
                },
            };
        }

        public static ApiVersion ParseAccept(in string accept, in ApiVersion defaultVersion)
        {
            // do we explicitly request a version? if not, return the default
            if (!string.IsNullOrEmpty(accept))
            {
                var types = accept.Split(',');
                foreach (var type in types)
                {
                    if (MediaTypeHeaderValue.TryParse(type, out var mediaType))
                    {
                        var profile = mediaType.Parameters.SingleOrDefault(p => p.Name == "profile");

                        // look up by custom attribute on the enum
                        FieldInfo[] fields = typeof(ApiVersion).GetFields();
                        var field = fields
                                        .SelectMany(f => f.GetCustomAttributes(
                                            typeof(ContextUriAttribute), false), (
                                                f, a) => new { Field = f, Att = a })
                                        .Where(a => ((ContextUriAttribute)a.Att)
                                            .ContextUri == profile?.GetUnescapedValue().Value).SingleOrDefault();
                        if (null != field)
                        {
                            return (ApiVersion)field.Field.GetRawConstantValue();
                        }
                    }
                }
            }
            return defaultVersion;
        }

        /// <summary>
        /// Returns a FileStreamResult of the image requested by id from the configured image location, processed using the supplied params  
        /// </summary>
        /// <param name="id">the id/name of the image appended to "Location" in the appsettings.json to form a Uri</param>
        /// <param name="region">the region of the image requested defined as either "full", "squa", "pct:" </param>
        /// <param name="size">Can be defined several ways as a percentage e.g. "pct:50" of as width and height e.g. "156,256" and with two predicates: ^ meaning upscale and ! meaning best? (not sure what that means yet). Using the upscale predicate and just supplying the width like so: "^100," would increase the width of the defined selection to 100 pixels(?I assume) whilst leaving the height unaltered.</param>
        /// <param name="rotation">defined as a number between 0 and 360 with the predicate of an exclamation mark e.g. "!270"</param>
        /// <param name="quality">Defined as either: @default, color, gray or bitonal.</param>
        /// <param name="format">Defined as either: jpg, tif, png, gif, jp2, pdf or webp</param>
        /// <returns>FileStreamResult</returns>
        [HttpGet("/{id}/{region}/{size}/{rotation}/{quality}.{format}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<Microsoft.AspNetCore.Mvc.ActionResult> ImageRequest(string id, string region, string size, string rotation, string quality, string format)
        {
            try
            {
                var (maxWidth, maxHeight, maxArea) = GetSizeConstraints(Conf);

                var request = ImageRequestValidator.Validate(
                                                                region,
                                                                size,
                                                                rotation,
                                                                quality,
                                                                format,
                                                                maxWidth,
                                                                maxHeight,
                                                                maxArea,
                                                                Conf.SupportedFormats(),
                                                                Conf.DefaultAPIVersion);
                return await request.Match<Task<Microsoft.AspNetCore.Mvc.ActionResult>>(
                    Right: async (r) =>
                    {
                        var imageUri = new Uri(new Uri(Conf.Location), id);
                        var allowSizeAboveFull = Conf.AllowSizeAboveFull;
                        var ms = await Processor.ProcessImage(imageUri, r, Conf.ImageQuality, allowSizeAboveFull, Conf.PdfMetadata);

                        Response
                            .Headers
                            .Add("Link",
                                new[] { $"<{Conf.DefaultAPIVersion.GetAttribute<ProfileUriAttribute>().ProfileUri}>;rel=\"profile\""
                                });
                        return new FileStreamResult(ms, r.Format.GetAttribute<ImageFormatMetadataAttribute>().MimeType)
                        {
#if !DEBUG
                            FileDownloadName = string.Format("{0}.{1}", id, format)
#endif
                        };
                    },
                    Left: async l => await new ValueTask<Microsoft.AspNetCore.Mvc.ActionResult>(BadRequest(l.ToProblemDetail(Conf.DefaultAPIVersion)))
                    );


            }
            catch (FileNotFoundException e)
            {
                _log.LogError("Unable to load source image @{FileName}", e.FileName);
                return NotFound(new { error = "Unable to load source image" });
            }
            catch (NotImplementedException e)
            {
                _log.LogInformation(e, "Un-implemented feature requested");
                return BadRequest(new { error = e.Message });
            }
            catch (ArgumentException e)
            {
                _log.LogError(e, "Error parsing argument");
                var errors = new Dictionary<string, string[]>() { { e.ParamName, new string[] { e.GetError() } } };
                var detail = e.GetError() + GetSpecLink(Conf.DefaultAPIVersion);
                return BadRequest(new ValidationProblemDetails(errors) { Detail = detail });
            }
            catch (FormatException e)
            {
                _log.LogError(e, "Error parsing argument");
                var detail = e.Message + GetSpecLink(Conf.DefaultAPIVersion);
                return BadRequest(new ValidationProblemDetails() { Detail = detail });
            }
            catch (Exception e)
            {
                _log.LogError(e, "Unexpected exception");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Points the user calling this API with just the image id to the info.json method
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Status 303 See Other response</returns>
        [HttpGet("/{id}", Name ="base")]
        public IActionResult BaseRedirect(string id)
        {
            return SeeOther("info.json", new { id = id });
        }

        /// <summary>
        /// Weirdly seems to simply return a 404 Status Not Found code, perhaps there was some intended future use for this?
        /// </summary>
        /// <returns>Status: Not Found 404</returns>
        [HttpGet("/favicon.ico")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult Favicon()
        {
            return NotFound();
        }

        [NonAction]
        protected IActionResult SeeOther(string routeName, object values)
        {
            var location = _generator.GetUriByName(_httpContextAccessor.HttpContext, routeName, values);
            //var location = Url.Link(routeName, values);
            HttpContext.Response.GetTypedHeaders().Location = new Uri(location);
            return StatusCode(StatusCodes.Status303SeeOther);
        }

        private (int, int, int) GetSizeConstraints(in ImageServer conf)
        {
            HttpContext.Items.TryGetValue("maxWidth", out object maxWidth);
            HttpContext.Items.TryGetValue("maxHeight", out object maxHeight);
            HttpContext.Items.TryGetValue("maxArea", out object maxArea);

            return ((maxWidth as int?) ?? conf.MaxWidth, (maxHeight as int?) ?? conf.MaxHeight, (maxArea as int?) ?? conf.MaxArea);
        }

        private static string GetSpecLink(ApiVersion apiVersion)
        {
            return $". Please consult the specification for details: {apiVersion.GetAttribute<ApiSpecificationAttribute>().SpecificationUri}";
        }

    }
}
