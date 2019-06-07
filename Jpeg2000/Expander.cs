using System;
using kdu_mni;
using SkiaSharp;
using Image.Common;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using C = TremendousIIIF.Common.Configuration;
using System.Threading.Tasks;
using System.Buffers;
using System.Threading;
using System.Runtime.InteropServices;

namespace Jpeg2000
{
    public static class J2KExpander
    {

        private static (KakaduMessage, Ckdu_message_formatter) InitialiseKakaduLogging(ILogger log)
        {
            //KakaduMessage sysout = new KakaduMessage(false, log);
            KakaduMessage message = new KakaduMessage(true, log);
            //Ckdu_message_formatter pretty_sysout = new Ckdu_message_formatter(sysout);
            Ckdu_message_formatter formatter = new Ckdu_message_formatter(message);

            //Ckdu_global_funcs.kdu_customize_warnings(pretty_sysout);
            Ckdu_global_funcs.kdu_customize_errors(formatter);
            return (message, formatter);
        }

        public static Metadata GetMetadata(Stream stream, ILogger log, Uri imageUri, int defaultTileWidth)
        {
            (var a, var b) = InitialiseKakaduLogging(log);
            using (a)
            using (b)
            using (var compSrc = new StreamCompressedSource(stream))
            using (var family_src = new Cjp2_family_src())
            using (var wrapped_src = new Cjpx_source())
            using (var tile_dims = new Ckdu_dims())
            {
                family_src.open(compSrc);
                if (1 != wrapped_src.open(family_src, true))
                {
                    family_src.close();
                    throw new IOException("Could not be read as JPEG2000");
                }
                log.LogDebug("Opened {@ImageURI}", imageUri);

                var meta_manager = wrapped_src.access_meta_manager();
                var node = meta_manager.access_root();
                bool hasGeoData = node.is_geojp2_uuid();
                node = node.get_next_descendant(null);
                while (node.exists())
                {
                    if (node.is_geojp2_uuid())
                    {
                        hasGeoData = true;
                        log.LogDebug("hasGeoData {@hasGeoData}", hasGeoData);
                    }
                    node = node.get_next_descendant(node);
                }

                var codestream = new Ckdu_codestream();
                codestream.create(compSrc);

                Ckdu_coords image_size = wrapped_src.access_layer(0).get_layer_size();

                int levels = codestream.get_min_dwt_levels();

                var imageSize = new SKPoint(image_size.x, image_size.y);

                codestream.get_tile_dims(new Ckdu_coords(0, 0), 0, tile_dims);

                var tileSize = new SKPoint(tile_dims.access_size().x, tile_dims.access_size().y);

                // if this wasn't encoded with Stiles={X,Y}, then the tile size will be image size
                if (tileSize == imageSize)
                {
                    using (var param = codestream.access_siz().access_cluster(Ckdu_global.COD_params))
                    {
                        bool usePrecincts = false;
                        param.get(Ckdu_global.Cuse_precincts, 0, 0, ref usePrecincts);
                        if (usePrecincts && tileSize == imageSize)
                        {
                            int[] precincts = new int[levels];
                            for (int i = 0; i < levels; i++)
                            {
                                param.get(Ckdu_global.Cprecincts, i, 0, ref precincts[i]);
                            }

                            tileSize = new SKPoint(precincts[0], precincts[0]);
                        }

                        // if no precincts defined, we should fall back on default size if it reports
                        // tile size as being image size
                        else
                        {
                            tileSize = new SKPoint(defaultTileWidth, defaultTileWidth);
                        }
                    }
                }

                codestream.destroy();
                family_src.close();
                wrapped_src.close();

                return new Metadata
                (
                    width: Convert.ToInt32(imageSize.X),
                    height: Convert.ToInt32(imageSize.Y),
                    scalingLevels: levels,
                    tileWidth: Convert.ToInt32(tileSize.X),
                    tileHeight: Convert.ToInt32(tileSize.Y),
                    hasGeoData: hasGeoData,
                    // TODO: make this an enum
                    qualities: 0,//colour.get_num_colours(),
                    sizes: null
                );


            }
        }

        public static (int, int, byte[]) GetGeoData(Stream stream, ILogger log, Uri imageUri, CancellationToken token = default)
        {

            //Ckdu_codestream codestream = new Ckdu_codestream();
            try
            {
                (var a, var b) = InitialiseKakaduLogging(log);
                using (a)
                using (b)
                using (var compSrc = new StreamCompressedSource(stream))
                using (var family_src = new Cjp2_family_src())
                using (var wrapped_src = new Cjpx_source())
                using (var tile_dims = new Ckdu_dims())
                {
                    family_src.open(compSrc);
                    if (1 != wrapped_src.open(family_src, true))
                    {
                        family_src.close();
                        throw new IOException("Could not be read as JPEG2000");
                    }
                    log.LogDebug("Opened {@ImageURI}", imageUri);
                    
                    //int ref_component = 0;

                    //codestream.create(wrapped_src.access_codestream(ref_component).open_stream());

                    //Ckdu_dims image_dims = new Ckdu_dims();

                    Ckdu_coords image_size = wrapped_src.access_layer(0).get_layer_size();
                    //Ckdu_coords image_size = image_dims.access_size();

                    var meta_manager = wrapped_src.access_meta_manager();
                    var node = meta_manager.access_root();

                    node = node.get_next_descendant(null);
                    using (Cjp2_input_box box = new Cjp2_input_box())
                    {
                        while (node.exists())
                        {
                            if (node.is_geojp2_uuid())
                            {
                                node.open_existing(box);
                                var boxLength = box.get_box_bytes();
                                var buf = ArrayPool<byte>.Shared.Rent((int)boxLength);
                                // reading the box includes reading the 16 byte UUID
                                box.read(buf, (int)boxLength);
                                var span = new Memory<byte>(buf);
                                var geotiff = span.Slice(16).ToArray();
                                ArrayPool<byte>.Shared.Return(buf);
                                family_src.close();
                                return (image_size.x, image_size.y, geotiff);
                            }
                            node = node.get_next_descendant(node);
                        }
                    }
                    family_src.close();
                    return (0, 0, null);
                }
            }
            finally
            {
                //if (codestream.exists())
                //    codestream.destroy();
            }
        }

        public static (ProcessState state, SKImage image) ExpandRegion(Stream stream, ILogger Log, Uri imageUri, ImageRequest request, bool allowSizeAboveFull, C.ImageQuality quality)
        {
            (var a, var b) = InitialiseKakaduLogging(Log);
            using (a)
            using (b)
            using (var compositor = new BitmapCompositor())
            using (var family_src = new Cjp2_family_src())
            using (var compSrc = new StreamCompressedSource(stream))
            using (var wrapped_src = new Cjp2_source())
            using (var srcImageDimensions = new Ckdu_dims())
            using (var srcRegionDimensions = new Ckdu_dims())

            using (var limiter = new Ckdu_quality_limiter(quality.WeightedRMSE))
            {

                try
                {
                    family_src.open(compSrc);

                    if (!wrapped_src.open(family_src))
                    {
                        family_src.close();
                        throw new IOException("Could not be read as JPEG2000");
                    }
                    Log.LogDebug("Opened {@ImageURI}", imageUri);
                    wrapped_src.read_header();

                    compositor.create(compSrc);

                    var quality_layers = 0;

                    // must set whole region and default scale before asking compositor to calculate dimensions
                    using (var initialLayer = compositor.add_ilayer(0, new Ckdu_dims(), new Ckdu_dims()))
                    {
                        compositor.set_scale(false, false, false, 1.0f);
                        compositor.get_total_composition_dims(srcImageDimensions);
                        srcRegionDimensions.assign(srcImageDimensions);

                        quality_layers = compositor.get_max_available_quality_layers();
                        // must remove it to properly target ROI
                        compositor.remove_ilayer(initialLayer, false);
                    }

                    var originalWidth = srcImageDimensions.access_size().x;
                    var originalHeight = srcImageDimensions.access_size().y;

                    Log.LogDebug("Size Source {@x} {@y}", originalWidth, originalHeight);

                    var state = ImageRequestInterpreter.GetInterpretedValues(request, originalWidth, originalHeight, allowSizeAboveFull);
                    Log.LogDebug("Image request {@Request}", state);

                    var res = wrapped_src.access_resolution();
                    ushort ppi_x = 96, ppi_y = 96;
                    ExtractDpi(res, ref ppi_x, ref ppi_y);

                    state.HorizontalResolution = ppi_x;
                    state.VerticalResolution = ppi_y;

                    var layers = quality.MaxQualityLayers;
                    if (layers < 0)
                        layers = quality_layers;
                    else if (layers == 0)
                        layers = Convert.ToInt32(Math.Ceiling(quality_layers / 2.0));

                    compositor.set_max_quality_layers(layers);
                    Log.LogDebug("Set max quality layers: {@Layers}", layers);

                    using (var imageSize = srcRegionDimensions.access_size())
                    using (var imagePosition = srcRegionDimensions.access_pos())
                    {
                        Log.LogDebug("Access size {@AccessSize}", imageSize);
                        Log.LogDebug("Access Position {@AccessPosition}", imagePosition);

                        float imageScale = 1;

                        var scale = state.OutputScale;
                        var scaleDiff = 0f;
                        imageScale = state.ImageScale;

                        // needs to be able to handle regions 
                        imageSize.x = Convert.ToInt32(Math.Round(state.RegionWidth / scale));
                        imageSize.y = Convert.ToInt32(Math.Round(state.RegionHeight / scale));

                        imagePosition.x = state.StartX;
                        imagePosition.y = state.StartY;

                        using (var extracted_dims = new Ckdu_dims())
                        using (var dstImageDimensions = new Ckdu_dims())
                        {
                            //dstImageDimensions.assign(srcImageDimensions);
                            extracted_dims.assign(srcImageDimensions);
                            extracted_dims.access_size().x = Convert.ToInt32(Math.Round(imageSize.x * imageScale));
                            extracted_dims.access_size().y = Convert.ToInt32(Math.Round(imageSize.y * imageScale));

                            //extracted_dims.access_pos().assign(imagePosition);
                            //extracted_dims.access_pos().x = state.StartX;
                            //extracted_dims.access_pos().y = state.StartY;
                            dstImageDimensions.access_pos().x = 0;
                            dstImageDimensions.access_pos().y = 0;
                            dstImageDimensions.access_size().x = state.OutputWidth;
                            dstImageDimensions.access_size().x = state.OutputHeight;

                            var viewSize = extracted_dims.access_size();
                            Log.LogDebug("add_ilayer extracted dimension: {@AccessPos}, {@AccessSize}, {@IsEmpty}, {@Scale}",
                                extracted_dims.access_pos(), extracted_dims.access_size(), extracted_dims.is_empty(), scale);

                            compositor.add_ilayer(0, extracted_dims, dstImageDimensions);
                            compositor.set_scale(false, false, false, scale);

                            // check_invalid_scale_code() resets to 0 after each call to set_scale(). must call get_total_composition_dims() before calling.
                            compositor.get_total_composition_dims(extracted_dims);
                            var checkScale = compositor.check_invalid_scale_code();
                            if (0 != checkScale)
                            {
                                Log.LogDebug("Scaling error: CheckScale {@CheckScale} Requested {@Scale} Dims {@Dims}", checkScale, scale, viewSize);
                                // we've come up with a scale factor which is (probably) way too small
                                // ask Kakadu to come up with a valid one that's close
                                var minScale = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_SMALL == checkScale ? scale : 0;
                                var maxScale = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_LARGE == checkScale ? scale : 1;

                                var optimal_scale = compositor.find_optimal_scale(extracted_dims, 0, minScale, maxScale);
                                scaleDiff = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_SMALL == checkScale ? optimal_scale - scale : scale - optimal_scale;
                                scale = optimal_scale;
                                compositor.set_scale(false, false, false, scale, scaleDiff);
                                compositor.get_total_composition_dims(extracted_dims);
                            }

                            compositor.get_total_composition_dims(extracted_dims);
                            Log.LogDebug("get_total_composition_dims extracted dimension: {@AccessPos}, {@AccessSize}, {@IsEmpty}, {@Scale}",
                                extracted_dims.access_pos(), extracted_dims.access_size(), extracted_dims.is_empty(), scale);
                            // check if the access size is the expected size as floating point rounding errors 
                            // might occur
                            //const float roundingValue = 0.0001f;
                            //if (((scale - roundingValue) * imageSize.x > 1 && (scale - roundingValue) * imageSize.y > 1) &&
                            //    (scale * imageSize.x != viewSize.x ||
                            //    scale * imageSize.y != viewSize.y))
                            //{
                            //    // attempt to correct by shifting rounding down
                            //    compositor.set_scale(false, false, false, 1, scale - roundingValue);

                            //    compositor.get_total_composition_dims(extracted_dims);
                            //    extracted_dims.access_size().x = Convert.ToInt32(Math.Round(imageSize.x * imageScale));
                            //    extracted_dims.access_size().y = Convert.ToInt32(Math.Round(imageSize.y * imageScale));
                            //    viewSize.Dispose();
                            //    viewSize = extracted_dims.access_size();
                            //}

                            checkScale = compositor.check_invalid_scale_code();
                            if (0 != checkScale)
                            {
                                Log.LogDebug("Scaling error: CheckScale {@CheckScale} Requested {@Scale} Dims {@Dims}", checkScale, scale, viewSize);
                                // we've come up with a scale factor which is (probably) way too small
                                // ask Kakadu to come up with a valid one that's close
                                var minScale = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_SMALL == checkScale ? scale : 0;
                                var maxScale = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_LARGE == checkScale ? scale : 1;

                                var optimal_scale = compositor.find_optimal_scale(extracted_dims, scale, scale, scale);
                                scaleDiff = Ckdu_global.KDU_COMPOSITOR_SCALE_TOO_SMALL == checkScale ? optimal_scale - scale : scale - optimal_scale;
                                scale = optimal_scale;
                                compositor.set_scale(false, false, false, scale);
                                compositor.get_total_composition_dims(extracted_dims);
                            }

                            compositor.set_buffer_surface(extracted_dims);
                            compositor.set_quality_limiting(limiter, quality.OutputDpi, quality.OutputDpi);
                            Log.LogDebug("Set quality limiting: {@Limiter}, {@HorizontalPPI}, {@VerticalPPI}",
                                limiter, quality.OutputDpi, quality.OutputDpi);

                            using (Ckdu_dims newRegion = new Ckdu_dims())
                            {
                                // we're only interested in the final composited image
                                while (compositor.process(0, newRegion, Ckdu_global.KDU_COMPOSIT_DEFER_REGION))
                                {
                                }
                                Log.LogDebug("compositor.is_processing_complete: {@prccomp}", compositor.is_processing_complete());

                                var compositorBuffer = compositor.GetCompositionBitmap(extracted_dims);
                                if (null == compositorBuffer)
                                {
                                    Log.LogError("Unable to composite region");
                                    throw new IOException("Unable to composite region of JPEG2000");
                                }
                                using (var bmp = compositorBuffer.AcquireBitmap())
                                {
                                    return (state, SKImage.FromBitmap(bmp));
                                }
                            }
                        }
                    }
                }
                finally
                {

                    if (family_src != null)
                        family_src.close();
                    if (wrapped_src != null)
                        wrapped_src.close();
                }
            }
        }

        private static void ExtractDpi(Cjp2_resolution resolution, ref ushort ppi_x, ref ushort ppi_y)
        {
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
    }
}
