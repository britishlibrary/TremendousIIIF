using Image.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        readonly static List<string> _validRightsDomains = new List<string> { "creativecommons.org", "rightsstatements.org" };

        public IIIFController(ILogger<IIIFController> log, ImageServer conf, ImageProcessing.ImageProcessing processor)
        {
            Conf = conf;
            Processor = processor;
            _log = log;
        }

        [HttpGet("/{id}/info.json", Name = "info.json")]

        [Produces("application/json")]
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
                var requestUrl = new Uri(host.Uri, Request.Path.ToUriComponent()).ToString();

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
                requestUri.Replace("/info.json", "") :
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

            switch (apiVersion)
            {
                case ApiVersion.v3_0:
                    return new Types.v3_0.ImageInfo(idUri, metadata, conf, maxWidth, maxHeight, maxArea, conf.EnableGeoService, manifestId, licenceUri);
                case ApiVersion.v2_1:
                default:
                    return new Types.v2_1.ImageInfo(metadata, conf, maxWidth, maxHeight, maxArea, conf.EnableGeoService, string.Format(conf.GeoDataBaseUri, id))
                    {
                        ID = idUri,
                    };
            }
        }

        public static ApiVersion ParseAccept(in string accept, in ApiVersion defaultVersion)
        {
            // do we explicitly request a version? if not, return the default
            if (!string.IsNullOrEmpty(accept))
            {
                var types = accept.Split(',');
                foreach (var type in types)
                {
                    var mediaType = MediaTypeHeaderValue.Parse(type);
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
            return defaultVersion;
        }

        [HttpGet("/{id}/{region}/{size}/{rotation}/{quality}.{format}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ImageRequest(string id, string region, string size, string rotation, string quality, string format)
        {
            try
            {
                (var maxWidth, var maxHeight, var maxArea) = GetSizeConstraints(Conf);

                var request = ImageRequestValidator.Validate(
                                                                region,
                                                                size,
                                                                rotation,
                                                                quality,
                                                                format,
                                                                maxWidth,
                                                                maxHeight,
                                                                maxArea,
                                                                Conf.SupportedFormats());
                return await request.Match<Task<ActionResult>>(
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
                    Left: async l => await new ValueTask<ActionResult>(BadRequest(l.ToProblemDetail(Conf.DefaultAPIVersion)))
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

        [HttpGet("/{id}")]
        public IActionResult BaseRedirect(string id)
        {
            return SeeOther("info.json", id);
        }

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
            var location = Url.Link(routeName, values);
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
