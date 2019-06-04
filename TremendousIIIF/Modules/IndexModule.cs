//using Nancy;
//using System.IO;
//using TremendousIIIF.Types;
//using TremendousIIIF.Validation;
//using System;
//using System.Net.Http;
//using Nancy.Owin;
//using Nancy.Responses;
//using Serilog;
//using TremendousIIIF.Common.Configuration;
//using System.Linq;
//using Nancy.Responses.Negotiation;
//using TremendousIIIF.Common;
//using System.Threading;
//using System.Threading.Tasks;

//namespace TremendousIIIF.Modules
//{
//    public class IIIFImageService : NancyModule
//    {
//        private ILogger Log { get; set; }
//        private ImageServer Conf { get; set; }
//        private ImageProcessing.ImageProcessing Processor { get; set; }
//        public IIIFImageService(ILogger log, ImageServer conf, ImageProcessing.ImageProcessing processor)
//        {
//            Log = log;
//            Conf = conf;
//            Processor = processor;

//            // Image Requests
//            Get("/ark:/{naan}/{id}/{region}/{size}/{rotation}/{quality}.{format}", async (parameters, token) => await ImageRequest(parameters, token));
//            Get("/{id}/{region}/{size}/{rotation}/{quality}.{format}", async (parameters, token) => await ImageRequest(parameters, token));

//            // info.json
//            Get("/ark:/{naan}/{id}/info.json", async (parameters, token) => await ImageInfo(parameters, token));
//            Get("/{id}/info.json", async (parameters, token) => await ImageInfo(parameters, token));
//            Get("/ark:/{naan}/{id}", async (parameters, token) =>
//            {
//                var newLocation = string.Format("{0}/info.json", Context.Request.Url.ToString());
//                return await new RedirectResponse(newLocation, RedirectResponse.RedirectType.SeeOther);
//            });
//            Get("/{id}", async (parameters, token) =>
//            {
//                var newLocation = string.Format("{0}/info.json", Context.Request.Url.ToString());
//                return await new RedirectResponse(newLocation, RedirectResponse.RedirectType.SeeOther);
//            });
//        }

//        private async Task<dynamic> ImageInfov3(dynamic parameters, CancellationToken token)
//        {

//            var filename = parameters.id;
//            var imageUri = new Uri(new Uri(Conf.Location), filename);
//            var requestId = Context.GetOwinEnvironment()["RequestId"] as string;
//            (var maxWidth, var maxHeight, var maxArea) = GetSizeConstraints(Conf);
//            var metadata = await Processor.GetImageInfo(_h, imageUri, Conf.DefaultTileWidth, requestId);

//            var full_id = Conf.BaseUri == null ?
//                Request.Url.ToString().Replace("/info.json", "") :
//                string.Format("{0}ark:/{1}/{2}", Conf.BaseUri.ToString(), parameters.naan, parameters.id);

//            var info = new Types.v3_1.ImageInfo(metadata, Conf, maxWidth, maxHeight, maxArea)
//            {
//                ID = full_id,
//            };

//            return await Negotiate
//                .WithAllowedMediaRange(new MediaRange("application/ld+json"))
//                    .WithAllowedMediaRange(new MediaRange("application/json"))
                    
//                    .WithHeader("Link", null) // hide nancy automatic Link: rel="alternative"
//                    .WithModel(info);
//        }
//        private async Task<dynamic> ImageInfo(dynamic parameters, CancellationToken token)
//        {
//            if (Conf.DefaultAPIVersion == ApiVersion.v3_0 )
//                return await ImageInfov3(parameters, token);

//            try
//            {
//                var filename = parameters.id;
//                var imageUri = new Uri(new Uri(Conf.Location), filename);
//                var requestId = Context.GetOwinEnvironment()["RequestId"] as string;
//                (var maxWidth, var maxHeight, var maxArea) = GetSizeConstraints(Conf);
//                var metadata = await Processor.GetImageInfo(imageUri, Conf.DefaultTileWidth, requestId);

//                var full_id = Conf.BaseUri == null ?
//                    Request.Url.ToString().Replace("/info.json", "") :
//                    string.Format("{0}ark:/{1}/{2}", Conf.BaseUri.ToString(), parameters.naan, parameters.id);

//                var info = new Types.v2_1.ImageInfo(metadata, Conf, maxWidth, maxHeight, maxArea)
//                {
//                    ID = full_id,
//                };

//                Log.Debug("{@ImageInfo}", info);

//                return await Negotiate
//                    .WithAllowedMediaRange(new MediaRange("application/json"))
//                    .WithAllowedMediaRange(new MediaRange("application/ld+json"))
//                    .WithHeader("Link", null) // hide nancy automatic Link: rel="alternative"
//                    .WithModel(info);
//            }
//            catch (FileNotFoundException e)
//            {
//                Log.Error("Unable to load source image @{FileName}", e.FileName);
//                return HttpStatusCode.NotFound;
//            }
//            catch (NotImplementedException e)
//            {
//                Log.Information(e, "Un-implemented feature requested");
//                return HttpStatusCode.BadRequest;
//            }
//            catch (ArgumentException e)
//            {
//                Log.Error(e, "Error parsing argument");
//                return HttpStatusCode.BadRequest;
//            }
//            catch (Exception e)
//            {
//                Log.Error(e, "Unexpected exception");
//                return HttpStatusCode.InternalServerError;
//            }
//        }
//        private async Task<dynamic> ImageRequest(dynamic parameters, CancellationToken token)
//        {
//            try
//            {
//                var filename = parameters.id;
//                var identifier = string.Format("ark:/{0}/{1}", parameters.naan, parameters.id);
//                (var maxWidth, var maxHeight, var maxArea) = GetSizeConstraints(Conf);

//                var requestId = Context.GetOwinEnvironment()["RequestId"] as string;

//                var request = ImageRequestValidator.Validate(
//                                                                parameters.region,
//                                                                parameters.size,
//                                                                parameters.rotation,
//                                                                parameters.quality,
//                                                                parameters.format,
//                                                                requestId,
//                                                                maxWidth,
//                                                                maxHeight,
//                                                                maxArea,
//                                                                Conf.SupportedFormats());
//                Log.Debug("{@Request}", request);
//                var imageUri = new Uri(new Uri(Conf.Location), filename);
//                var allowSizeAboveFull = Conf.AllowSizeAboveFull;
//                Stream ms = await Processor.ProcessImage(imageUri, request, Conf.ImageQuality, allowSizeAboveFull, Conf.PdfMetadata);
//                ImageFormat f = request.Format;
//                string mimetype = f.GetAttribute<ImageFormatMetadataAttribute>().MimeType;
//                return new StreamResponse(() => ms, mimetype)
//                    .WithHeader("Link", string.Format("<{0}>;rel=\"profile\"", "http://iiif.io/api/image/3/context.json"))
//#if !DEBUG
//                        .AsAttachment(string.Format("{0}.{1}", (string)parameters.id, (string)parameters.format));
//#else
//                    ;
//#endif
//            }
//            catch (FileNotFoundException e)
//            {
//                Log.Error("Unable to load source image @{FileName}", e.FileName);
//                return HttpStatusCode.NotFound;
//            }
//            catch (NotImplementedException e)
//            {
//                Log.Information(e, "Un-implemented feature requested");
//                return Response.AsJson(e.Message, HttpStatusCode.BadRequest);
//            }
//            catch (ArgumentException e)
//            {
//                Log.Error(e, "Error parsing argument");
//                return Response.AsJson(e.Message, HttpStatusCode.BadRequest);
//            }
//            catch (FormatException e)
//            {
//                Log.Error(e, "Error parsing argument");
//                return Response.AsJson(e.Message, HttpStatusCode.BadRequest);
//            }
//            catch (Exception e)
//            {
//                Log.Error(e, "Unexpected exception");
//                return HttpStatusCode.InternalServerError;
//            }
//        }


//        public (int, int, int) GetSizeConstraints(in ImageServer conf)
//        {
//            var maxWidth = Context.GetOwinEnvironment().Where(t => t.Key == "maxWidth").Select(t => t.Value as int?).SingleOrDefault();
//            var maxHeight = Context.GetOwinEnvironment().Where(t => t.Key == "maxHeight").Select(t => t.Value as int?).SingleOrDefault();
//            var maxArea = Context.GetOwinEnvironment().Where(t => t.Key == "maxArea").Select(t => t.Value as int?).SingleOrDefault();

//            return (maxWidth ?? conf.MaxWidth, maxHeight ?? conf.MaxHeight, maxArea ?? conf.MaxArea);
//        }
//    }

//}