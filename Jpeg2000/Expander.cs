using System;
using kdu_mni;
using SkiaSharp;
using Image.Common;
using System.Net.Http;
using Serilog;
using System.IO;
using C = TremendousIIIF.Common.Configuration;
using System.Threading.Tasks;

namespace Jpeg2000
{
    public class J2KExpander
    {
        private static void InitialiseKakaduLogging(ILogger log)
        {
            KakaduMessage sysout = new KakaduMessage(false, log);
            KakaduMessage syserr = new KakaduMessage(true, log);
            Ckdu_message_formatter pretty_sysout = new Ckdu_message_formatter(sysout);
            Ckdu_message_formatter pretty_syserr = new Ckdu_message_formatter(syserr);

            Ckdu_global_funcs.kdu_customize_warnings(pretty_sysout);
            Ckdu_global_funcs.kdu_customize_errors(pretty_syserr);
        }
        public static async Task<Metadata> GetMetadata(HttpClient client, ILogger log, Uri imageUri, int defaultTileWidth, string requestId)
        {

            InitialiseKakaduLogging(log);

            Ckdu_codestream codestream = new Ckdu_codestream();
            try
            {
                using (var family_src = new JPEG2000Source(log, requestId))
                using (var wrapped_src = new Cjpx_source())
                using (var jp2_source = new Cjp2_source())
                {
                    await family_src.Initialise(client, imageUri, false);
                    family_src.Open(imageUri);

                    int success = wrapped_src.open(family_src, true);
                    if (success < 1)
                    {
                        family_src.close();
                        wrapped_src.close();
                        throw new IOException("could not be read as JPEG2000");
                    }

                    //jp2_source.open(family_src);
                    //while (!jp2_source.read_header()) ;

                    int ref_component = 0;

                    codestream.create(wrapped_src.access_codestream(ref_component).open_stream());

                    Ckdu_dims image_dims = new Ckdu_dims();
                    codestream.get_dims(ref_component, image_dims);
                    Ckdu_coords image_size = image_dims.access_size();

                    int levels = codestream.get_min_dwt_levels();
                    var imageSize = new SKPoint(image_size.x, image_size.y);

                    Ckdu_dims tile_dims = new Ckdu_dims();
                    codestream.get_tile_dims(new Ckdu_coords(0, 0), ref_component, tile_dims);

                    var tileSize = new SKPoint(tile_dims.access_size().x, tile_dims.access_size().y);
                    int[] precincts = new int[levels];
                    // get precinct info
                    using (var param = codestream.access_siz().access_cluster("COD"))
                    {
                        bool usePrecincts = false;
                        param.get("Cuse_precincts", 0, 0, ref usePrecincts);
                        if (usePrecincts)
                        {
                            for (int i = 0; i < levels; i++)
                            {
                                param.get("Cprecincts", i, 0, ref precincts[i]);
                            }
                            tileSize = new SKPoint(precincts[0], precincts[0]);
                        }
                        // if no precincts defined, we should fall back on default size if it reports
                        // tile size as being image size
                        else if (tileSize == imageSize)
                        {
                            tileSize = new SKPoint(defaultTileWidth, defaultTileWidth);
                        }
                    }

                    tile_dims.Dispose();
                    image_size.Dispose();
                    image_dims.Dispose();

                    if (codestream.exists())
                        codestream.destroy();
                    family_src.close();

                    return new Metadata
                    {
                        Width = Convert.ToInt32(imageSize.X),
                        Height = Convert.ToInt32(imageSize.Y),
                        ScalingLevels = levels,
                        TileWidth = Convert.ToInt32(tileSize.X),
                        TileHeight = Convert.ToInt32(tileSize.Y)
                    };

                }
            }
            finally
            {
                if (codestream.exists())
                    codestream.destroy();
            }

        }
        public static async Task<(ProcessState state, SKImage image)> ExpandRegion(HttpClient client, ILogger Log, Uri imageUri, ImageRequest request, bool allowSizeAboveFull, C.ImageQuality quality)
        {
            InitialiseKakaduLogging(Log);

            using (var compositor = new BitmapCompositor())
            //using (var env = new Ckdu_thread_env())
            using (var family_src = new JPEG2000Source(Log, request.RequestId))
            using (var wrapped_src = new Cjpx_source())
            using (var imageDimensions = new Ckdu_dims())
            using (var limiter = new Ckdu_quality_limiter(quality.WeightedRMSE))
            {
                await family_src.Initialise(client, imageUri, false);
                Ckdu_codestream codestream = new Ckdu_codestream();
                try
                {
                    int num_threads = Ckdu_global_funcs.kdu_get_num_processors();
                    //env.create();

                    //for (int nt = 1; nt < num_threads; nt++)
                    //{
                    //    if (!env.add_thread())
                    //    {
                    //        num_threads = nt;
                    //    }
                    //}
                    Log.Debug("Created {@NumThreads} threads", num_threads);
                    family_src.Open(imageUri);
                    Log.Debug("Opened {@ImageURI}", imageUri);

                    int success = wrapped_src.open(family_src, true);
                    if (success < 1)
                    {
                        family_src.close();
                        wrapped_src.close();
                        throw new IOException("could not be read as JPEG2000");
                    }
                    Log.Debug("Wrapped Source {@Sucess}", success);

                    if (wrapped_src == null)
                    {
                        throw new IOException("could not be read as JPEG2000");
                    }
                    int ref_component = 0;
                    var input_box = wrapped_src.access_codestream(ref_component, true);
                    if (null == input_box)
                    {
                        throw new IOException("Unable to open access codestream");
                    }

                    var input_stream = input_box.open_stream();

                    codestream.create(input_stream, null);
                    Log.Debug("Codestream created");

                    codestream.set_fast();

                    int originalWidth, originalHeight;
                    using (Ckdu_dims original_dims = new Ckdu_dims())
                    {
                        codestream.get_dims(ref_component, original_dims);
                        using (Ckdu_coords original_size = original_dims.access_size())
                        {
                            originalWidth = original_size.x;
                            originalHeight = original_size.y;
                        }
                    }

                    var kt = codestream.open_tile(new Ckdu_coords(0, 0), null);
                    var quality_layers = codestream.get_max_tile_layers();
                    var layers = quality.MaxQualityLayers;
                    if (layers < 0)
                        layers = quality_layers;
                    else if (layers == 0)
                        layers = Convert.ToInt32(Math.Ceiling(quality_layers / 2.0));

                    ushort ppi_x = 96, ppi_y = 96;

                    if (wrapped_src.access_layer(0).exists())
                    {
                        Log.Debug("Access Layer exists");
                        var accessLayer = wrapped_src.access_layer(0);
                        var resolution = accessLayer.access_resolution();
                        if (resolution.exists())
                        {
                            bool for_display = false;
                            float ypels_per_metre = resolution.get_resolution(for_display);
                            if (ypels_per_metre <= 0.0F)
                            {
                                for_display = true;
                                ypels_per_metre = resolution.get_resolution(for_display);
                                if (ypels_per_metre <= 0.0F)
                                {
                                    ypels_per_metre = 1.0F;
                                }
                            }

                            float xpels_per_metre = ypels_per_metre * resolution.get_aspect_ratio(for_display);

                            ppi_x = Convert.ToUInt16(Math.Ceiling(xpels_per_metre * 0.0254));
                            ppi_y = Convert.ToUInt16(Math.Ceiling(ypels_per_metre * 0.0254));
                        }
                    }

                    compositor.create(wrapped_src);
                    Log.Debug("Compositor created");
                    //compositor.set_thread_env(env, null);
                    compositor.get_total_composition_dims(imageDimensions);
                    Log.Debug("Get total composition dims {@ImageDims}", imageDimensions);
                    Ckdu_coords imageSize = imageDimensions.access_size();
                    Log.Debug("Access size {@AccessSize}", imageSize);
                    Ckdu_coords imagePosition = imageDimensions.access_pos();
                    Log.Debug("Access Position {@AccessPosition}", imagePosition);

                    float imageScale = 1;


                    int codestreams = 0;

                    Log.Debug("Family Src top level {@IsTopLevelComplete}", family_src.is_top_level_complete());
                    Log.Debug("Family Src codestream {@Codestream}", family_src.is_codestream_main_header_complete(0));
                    Log.Debug("Family Src exists {@Exists}", family_src.exists());


                    var codestream_count = wrapped_src.count_codestreams(ref codestreams);
                    Log.Debug("Counted codestreams {@Codestreams} {@CodestreamCount}", codestreams, codestream_count);




                    var state = ImageRequestInterpreter.GetInterpretedValues(request, originalWidth, originalHeight, allowSizeAboveFull);
                    state.HorizontalResolution = ppi_x;
                    state.VerticalResolution = ppi_y;
                    Log.Debug("Image request {@Request}", state);
                    var scale = state.OutputScale;
                    var scaleDiff = 0f;
                    imageScale = state.ImageScale;

                    // needs to be able to handle regions 
                    imageSize.x = Convert.ToInt32(Math.Round(state.RegionWidth / scale));
                    imageSize.y = Convert.ToInt32(Math.Round(state.RegionHeight / scale));

                    imagePosition.x = state.StartX;
                    imagePosition.y = state.StartY;

                    Ckdu_dims extracted_dims = new Ckdu_dims();
                    extracted_dims.assign(imageDimensions);
                    extracted_dims.access_size().x = Convert.ToInt32(Math.Round(imageSize.x * imageScale));
                    extracted_dims.access_size().y = Convert.ToInt32(Math.Round(imageSize.y * imageScale));
                    var viewSize = extracted_dims.access_size();
                    Log.Debug("add_ilayer extracted dimension: {@AccessPos}, {@AccessSize}, {@IsEmpty}, {@Scale}", extracted_dims.access_pos(), extracted_dims.access_size(), extracted_dims.is_empty(), scale);
                    compositor.add_ilayer(0, extracted_dims, extracted_dims);

                    compositor.set_scale(false, false, false, scale);

                    compositor.get_total_composition_dims(extracted_dims);
                    Log.Debug("get_total_composition_dims extracted dimension: {@AccessPos}, {@AccessSize}, {@IsEmpty}, {@Scale}", extracted_dims.access_pos(), extracted_dims.access_size(), extracted_dims.is_empty(), scale);
                    // check if the access size is the expected size as floating point rounding errors 
                    // might occur
                    const float roundingValue = 0.0001f;
                    if (((scale - roundingValue) * imageSize.x > 1 && (scale - roundingValue) * imageSize.y > 1) &&
                        (scale * imageSize.x != viewSize.x ||
                        scale * imageSize.y != viewSize.y))
                    {
                        // attempt to correct by shifting rounding down
                        compositor.set_scale(false, false, false, 1, scale - roundingValue);

                        compositor.get_total_composition_dims(extracted_dims);
                        extracted_dims.access_size().x = Convert.ToInt32(Math.Round(imageSize.x * imageScale));
                        extracted_dims.access_size().y = Convert.ToInt32(Math.Round(imageSize.y * imageScale));
                        viewSize = extracted_dims.access_size();
                    }

                    var checkScale = compositor.check_invalid_scale_code();
                    if (0 != checkScale)
                    {
                        // we've come up with a scale factor which is (probably) way too small
                        // ask Kakadu to come up with a valid one that's close
                        var minScale = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_SMALL == checkScale ? scale : 0;
                        var maxScale = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_LARGE == checkScale ? scale : 1;

                        var optimal_scale = compositor.find_optimal_scale(extracted_dims, 0, minScale, maxScale);
                        scaleDiff = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_SMALL == checkScale ? optimal_scale - scale : scale - optimal_scale;
                        scale = optimal_scale;
                        compositor.set_scale(false, false, false, scale);
                        compositor.get_total_composition_dims(extracted_dims);
                    }
                    viewSize = extracted_dims.access_size();
                    Log.Debug("Extracted dimension: {@AccessPos}, {@AccessSize}, {@IsEmpty}, {@Scale}", extracted_dims.access_pos(), extracted_dims.access_size(), extracted_dims.is_empty(), scale);
                    compositor.set_buffer_surface(extracted_dims);
                    compositor.set_quality_limiting(limiter, quality.OutputDpi, quality.OutputDpi);
                    Log.Debug("Set quality limiting: {@Limiter}, {@HorizontalPPI}, {@VerticalPPI}", limiter, quality.OutputDpi, quality.OutputDpi);
                    compositor.set_max_quality_layers(layers);
                    Log.Debug("Set max quality layers: {@Layers}", layers);
                    var compositorBuffer = compositor.GetCompositionBitmap(extracted_dims);

                    using (Ckdu_dims newRegion = new Ckdu_dims())
                    {
                        // we're only interested in the final composited image
                        while (compositor.process(256000, newRegion))
                        {
                        }

                        using (var bmp = compositorBuffer.AcquireBitmap())
                        {
                            return (state, SKImage.FromBitmap(bmp));
                        }
                    }
                }
                finally
                {
                    if (codestream.exists())
                        codestream.destroy();

                    //if (env.exists())
                    //    env.destroy();

                    if (family_src != null)
                        family_src.close();
                    else if (wrapped_src != null)
                        wrapped_src.close();
                }
            }
        }
    }
}
