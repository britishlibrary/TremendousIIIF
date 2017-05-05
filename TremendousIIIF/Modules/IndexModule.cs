using Nancy;
using System.IO;
using TremendousIIIF.Types;
using TremendousIIIF.Validation;
using MimeTypes;
using System;
using System.Net.Http;
using Nancy.Owin;
using Nancy.Responses;
using Serilog;
using System.Collections.Generic;
using Image.Common;
using Microsoft.Extensions.Configuration;
using TremendousIIIF.Common.Configuration;
using System.Linq;
using Nancy.Responses.Negotiation;

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
                    var request = ImageRequestValidator.Validate(identifier,
                                                                    parameters.region,
                                                                    parameters.size,
                                                                    parameters.rotation,
                                                                    parameters.quality,
                                                                    parameters.format,
                                                                    conf.MaxWidth,
                                                                    conf.MaxHeight,
                                                                    conf.MaxArea);
                    request.RequestId = Context.GetOwinEnvironment()["RequestId"] as string;
                    log.Debug("@{Request}", request);
                    // pipeline is
                    // validation -> extraction -> transformation
                    // Region THEN Size THEN Rotation THEN Quality THEN Format
                    var imageUri = new Uri(new Uri(conf.Location), filename);
                    var quality = conf.ImageQuality.GetOutputFormatQuality(request.Format);
                    var allowSizeAboveFull = conf.AllowSizeAboveFull;
                    var processor = new ImageProcessing.ImageProcessing { HttpClient = httpClient, Log = log };
                    MemoryStream ms = await processor.ProcessImage(imageUri, request, conf.ImageQuality, allowSizeAboveFull);
                    string mimetype = MimeTypeMap.GetMimeType(parameters.format);
                    return new StreamResponse(() => ms, mimetype)
                        .WithHeader("Link", string.Format("<{0}>;rel=\"profile\"", new ImageInfo().Profile.First()));
                }
                catch (FileNotFoundException e)
                {
                    log.Error("Unable to load source image @{FileName}", e.FileName);
                    return HttpStatusCode.NotFound;
                }
                catch (NotImplementedException e)
                {
                    log.Information(e, "Un-implemented feature requested");
                    var response = (Response)e.Message;
                    response.ContentType = "application/json";
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }
                catch (ArgumentException e)
                {
                    log.Error(e, "Error parsing argument");
                    var response = (Response)e.Message;
                    response.ContentType = "application/json";
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }
                catch (FormatException e)
                {
                    log.Error(e, "Error parsing argument");
                    var response = (Response)e.Message;
                    response.ContentType = "application/json";
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
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
                    var identifier = string.Format("ark:/{0}/{1}", parameters.naan, parameters.id);
                    var imageUri = new Uri(new Uri(conf.Location), filename);
                    var requestId = Context.GetOwinEnvironment()["RequestId"] as string;
                    var processor = new ImageProcessing.ImageProcessing { HttpClient = httpClient, Log = log };
                    var metadata = await processor.GetImageInfo(imageUri, conf.DefaultTileWidth, requestId);

                    var tile = new Tile()
                    {
                        Width = metadata.TileWidth,
                        Height = metadata.TileHeight
                    };
                    for (int i = 0; i < metadata.ScalingLevels; i++)
                    {
                        tile.ScaleFactors.Add(Convert.ToInt32(Math.Pow(2, i)));
                    }

                    var full_id = conf.BaseUri == null ?
                        Request.Url.ToString().Replace("/info.json", "") :
                        string.Format("{0}ark:/{1}/{2}", conf.BaseUri.ToString(), parameters.naan, parameters.id);

                    var info = new ImageInfo
                    {
                        Height = metadata.Height,
                        Width = metadata.Width,

                        ID = full_id,
                        Tiles = new List<Tile> { tile }
                    };
                    info.Profile.Add(new ServiceProfile(conf.AllowSizeAboveFull)
                    {
                        MaxWidth = conf.MaxWidth == int.MaxValue ? default(int) : conf.MaxWidth,
                        MaxHeight = conf.MaxHeight == int.MaxValue ? default(int) : conf.MaxHeight
                    });
                    log.Debug("@{ImageInfo}", info);

                    return await Negotiate
                        .WithAllowedMediaRange(new MediaRange("application/ld+json"))
                        .WithAllowedMediaRange(new MediaRange("application/json"))
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
    }
}