using Nancy;
using System.IO;
using TremendousIIIF.Types;
using TremendousIIIF.Validation;
using System;
using System.Net.Http;
using Nancy.Owin;
using Nancy.Responses;
using Serilog;
using TremendousIIIF.Common.Configuration;
using System.Linq;
using Nancy.Responses.Negotiation;
using TremendousIIIF.Common;

namespace TremendousIIIF.Modules
{
    public class IIIFImageService : NancyModule
    {
        public IIIFImageService(HttpClient httpClient, ILogger log, ImageServer conf)
        {
            Get("/ark:/{naan}/{id}/{region}/{size}/{rotation}/{quality}.{format}", async (parameters, token) =>
            {
                try
                {
                    var filename = parameters.id;
                    var identifier = string.Format("ark:/{0}/{1}", parameters.naan, parameters.id);
                    (var maxWidth, var maxHeight, var maxArea) = GetSizeConstraints(conf);

                    var request = ImageRequestValidator.Validate(identifier,
                                                                    parameters.region,
                                                                    parameters.size,
                                                                    parameters.rotation,
                                                                    parameters.quality,
                                                                    parameters.format,
                                                                    maxWidth,
                                                                    maxHeight,
                                                                    maxArea,
                                                                    conf.SupportedFormats());
                    request.RequestId = Context.GetOwinEnvironment()["RequestId"] as string;

                    log.Debug("{@Request}", request);
                    var imageUri = new Uri(new Uri(conf.Location), filename);
                    var allowSizeAboveFull = conf.AllowSizeAboveFull;
                    var processor = new ImageProcessing.ImageProcessing { HttpClient = httpClient, Log = log };
                    MemoryStream ms = await processor.ProcessImage(imageUri, request, conf.ImageQuality, allowSizeAboveFull, conf.PdfMetadata);
                    string mimetype = request.Format.GetAttribute<ImageFormatMetadataAttribute>().MimeType;
                    return new StreamResponse(() => ms, mimetype)
                        .WithHeader("Link", string.Format("<{0}>;rel=\"profile\"", new ImageInfo().Profile.First()))
#if !DEBUG
                        .AsAttachment(string.Format("{0}.{1}", (string)parameters.id, (string)parameters.format));
#else
                    ;
#endif
                }
                catch (FileNotFoundException e)
                {
                    log.Error("Unable to load source image @{FileName}", e.FileName);
                    return HttpStatusCode.NotFound;
                }
                catch (NotImplementedException e)
                {
                    log.Information(e, "Un-implemented feature requested");
                    return Response.AsJson(e.Message, HttpStatusCode.BadRequest);
                }
                catch (ArgumentException e)
                {
                    log.Error(e, "Error parsing argument");
                    return Response.AsJson(e.Message, HttpStatusCode.BadRequest);
                }
                catch (FormatException e)
                {
                    log.Error(e, "Error parsing argument");
                    return Response.AsJson(e.Message, HttpStatusCode.BadRequest);
                }
                catch (Exception e)
                {
                    log.Error(e, "Unexpected exception");
                    return HttpStatusCode.InternalServerError;
                }
            });

            Get("/ark:/{naan}/{id}/info.json", async (parameters, token) =>
            {
                try
                {
                    var filename = parameters.id;
                    var imageUri = new Uri(new Uri(conf.Location), filename);
                    var requestId = Context.GetOwinEnvironment()["RequestId"] as string;
                    (var maxWidth, var maxHeight, var maxArea) = GetSizeConstraints(conf);
                    var processor = new ImageProcessing.ImageProcessing { HttpClient = httpClient, Log = log };
                    var metadata = await processor.GetImageInfo(imageUri, conf.DefaultTileWidth, requestId);

                    var full_id = conf.BaseUri == null ?
                        Request.Url.ToString().Replace("/info.json", "") :
                        string.Format("{0}ark:/{1}/{2}", conf.BaseUri.ToString(), parameters.naan, parameters.id);

                    var info = new ImageInfo(metadata, conf, maxWidth, maxHeight, maxArea)
                    {
                        ID = full_id,
                    };

                    log.Debug("{@ImageInfo}", info);

                    return await Negotiate
                        .WithAllowedMediaRange(new MediaRange("application/json"))
                        .WithAllowedMediaRange(new MediaRange("application/ld+json"))
                        .WithHeader("Link", null) // hide nancy automatic Link: rel="alternative"
                        .WithModel(info);
                }
                catch (FileNotFoundException e)
                {
                    log.Error("Unable to load source image @{FileName}", e.FileName);
                    return HttpStatusCode.NotFound;
                }
                catch (NotImplementedException e)
                {
                    log.Information(e, "Un-implemented feature requested");
                    return HttpStatusCode.BadRequest;
                }
                catch (ArgumentException e)
                {
                    log.Error(e, "Error parsing argument");
                    return HttpStatusCode.BadRequest;
                }
                catch (Exception e)
                {
                    log.Error(e, "Unexpected exception");
                    return HttpStatusCode.InternalServerError;
                }
            });

            Get("/ark:/{naan}/{id}", (parameters) =>
            {
                var newLocation = string.Format("{0}/info.json", Context.Request.Url.ToString());
                return new RedirectResponse(newLocation, RedirectResponse.RedirectType.SeeOther);
            });
        }

        public (int, int, int) GetSizeConstraints(ImageServer conf)
        {
            var maxWidth = Context.GetOwinEnvironment().Where(t => t.Key == "maxWidth").Select(t => t.Value as int?).SingleOrDefault();
            var maxHeight = Context.GetOwinEnvironment().Where(t => t.Key == "maxHeight").Select(t => t.Value as int?).SingleOrDefault();
            var maxArea = Context.GetOwinEnvironment().Where(t => t.Key == "maxArea").Select(t => t.Value as int?).SingleOrDefault();

            return (maxWidth ?? conf.MaxWidth, maxHeight ?? conf.MaxHeight, maxArea ?? conf.MaxArea);
        }
    }

}