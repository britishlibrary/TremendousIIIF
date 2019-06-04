using System;
using Dimensions = System.ValueTuple<int, int, float>;

namespace Image.Common
{
    public static class ImageRequestInterpreter
    {
        /// <summary>
        /// Calculates the region, offset and final output image size
        /// </summary>
        /// <param name="request"></param>
        /// <param name="originalWidth">Original width (pixels) of the source image</param>
        /// <param name="originalHeight">Original height (pixels) of the source image</param>
        /// <param name="allowUpscaling">Allow output image dimensions to exceed that of the source image, but constrained by <see cref="ImageRequest.MaxWidth"/>,<see cref="ImageRequest.MaxHeight"/>,<see cref="ImageRequest.MaxArea"/></param>
        /// <returns></returns>
        static public ProcessState GetInterpretedValues(in ImageRequest request, int originalWidth, int originalHeight, bool allowUpscaling)
        {
            request.CheckRequest(allowUpscaling);
            ProcessState state = new ProcessState();

            state.StartX = state.StartY = state.RegionHeight = state.RegionWidth = 0;
            state.OutputScale = state.ImageScale = 1;
            switch (request.Region.Mode)
            {
                case ImageRegionMode.PercentageRegion:
                    state.StartX = Convert.ToInt32(request.Region.X / 100 * originalWidth);
                    state.StartY = Convert.ToInt32(request.Region.Y / 100 * originalHeight);
                    state.OutputWidth = state.RegionWidth = Convert.ToInt32(request.Region.Width / 100 * originalWidth);
                    state.OutputHeight = state.RegionHeight = Convert.ToInt32(request.Region.Height / 100 * originalHeight);
                    break;
                case ImageRegionMode.Region:
                    state.StartX = Convert.ToInt32(request.Region.X);
                    state.StartY = Convert.ToInt32(request.Region.Y);
                    // If the request specifies a region which extends beyond the dimensions of the full image as reported in the image information document, 
                    // then the service SHOULD return an image cropped at the image’s edge, rather than adding empty space.
                    // https://preview.iiif.io/api/image-prezi-rc2/api/image/3.0/#41-region
                    state.OutputWidth = state.RegionWidth = Math.Min(Convert.ToInt32(request.Region.Width), originalWidth);
                    state.OutputHeight = state.RegionHeight = Math.Min(Convert.ToInt32(request.Region.Height), originalHeight);
                    break;
                case ImageRegionMode.Square:
                    state.OutputWidth = state.OutputHeight = state.RegionHeight = state.RegionWidth = Math.Min(originalWidth, originalHeight);
                    // pick the middle of the image
                    state.StartX = state.RegionWidth == originalWidth ? 0 : Convert.ToInt32(Math.Round((originalWidth - originalHeight) / 2f));
                    state.StartY = state.RegionHeight == originalHeight ? 0 : Convert.ToInt32(Math.Round((originalHeight - originalWidth) / 2f));

                    break;
                default:
                    state.OutputWidth = state.RegionWidth = originalWidth;
                    state.OutputHeight = state.RegionHeight = originalHeight;
                    break;
            }

            CheckBounds(state);

            switch (request.Size.Mode)
            {
                case ImageSizeMode.Max:
                case ImageSizeMode.Full:
                    // no changes needed as scale and OutputWidth/Height are set
                    break;
                case ImageSizeMode.PercentageScaled:
                    state.OutputWidth = Convert.ToInt32(state.RegionWidth * request.Size.Percent.Value);
                    state.OutputHeight = Convert.ToInt32(state.RegionHeight * request.Size.Percent.Value);
                    state.OutputScale = request.Size.Percent.Value;
                    break;
                case ImageSizeMode.Distort:
                    float scaledx, scaledy = 1f;

                    scaledy = request.Size.Height / (float)state.RegionHeight;
                    scaledx = request.Size.Width / (float)state.RegionWidth;

                    state.OutputWidth = Convert.ToInt32(state.RegionWidth * scaledx);
                    state.OutputHeight = Convert.ToInt32(state.RegionHeight * scaledy);
                    if (scaledx < scaledy)
                    {
                        state.OutputScale = scaledx;
                    }
                    else
                    {
                        state.OutputScale = scaledy;
                    }
                    if (request.Region.Mode != ImageRegionMode.Full)
                    {
                        if (scaledx < scaledy)
                            state.ImageScale = scaledx;
                        else
                            state.ImageScale = scaledy;
                    }
                    break;
                case ImageSizeMode.MaintainAspectRatio:
                    if (request.Size.Width != 0 && request.Size.Height == 0)
                    {
                        var scale = request.Size.Width / (float)state.RegionWidth;
                        state.OutputWidth = Convert.ToInt32(state.RegionWidth * scale);
                        state.OutputHeight = Convert.ToInt32(state.RegionHeight * scale);
                        state.OutputScale = scale;
                    }
                    else if (request.Size.Width == 0 && request.Size.Height != 0)
                    {
                        var scale = request.Size.Height / (float)state.RegionHeight;
                        state.OutputWidth = Convert.ToInt32(state.RegionWidth * scale);
                        state.OutputHeight = Convert.ToInt32(state.RegionHeight * scale);
                        state.OutputScale = scale;
                    }
                    else
                    {
                        var originalScale = originalWidth / (float)originalHeight;
                        var scale = Math.Min((request.Size.Height / (float)state.RegionHeight), (request.Size.Width / (float)state.RegionWidth));
                        state.OutputWidth = Convert.ToInt32(state.RegionWidth * scale);
                        state.OutputHeight = Convert.ToInt32(state.RegionHeight * scale);
                        state.OutputScale = scale;

                        if (request.Region.Mode != ImageRegionMode.Full)
                        {
                            state.ImageScale = scale;
                        }
                    }
                    break;
            }

            // final bounds reduction
            float max_scale = 1f;
            (state.OutputWidth, state.OutputHeight, max_scale)
                = ScaleOutput(request.MaxWidth, request.MaxHeight, state.OutputWidth, state.OutputHeight, request.Size.Upscale);
            state.OutputScale = Math.Min(max_scale, state.ImageScale);

            state.CheckBounds();

            return state;
        }

        /// <summary>
        /// Scale final output to be within the <paramref name="maxWidth"/> or <paramref name="maxHeight"/>
        /// </summary>
        /// <param name="maxWidth">Maximum width (pixels) of the output image</param>
        /// <param name="maxHeight">Maximum height (pixels) of the output image</param>
        /// <param name="requestedWidth">Requested width (pixels) of the output image</param>
        /// <param name="requestedHeight">Requested height (pixels) of the output image</param>
        /// <param name="allowUpscaling">Allow output image dimensions to exceed those of the source image</param>
        /// <returns></returns>
        private static Dimensions ScaleOutput(int maxWidth, int maxHeight, int requestedWidth, int requestedHeight, bool allowUpscaling)
        {

            float maxWscale = maxWidth == int.MaxValue ? 1 : (float)maxWidth / requestedWidth;
            float maxHscale = maxHeight == int.MaxValue ? 1 : (float)maxHeight / requestedHeight;

            var requestedScale = (requestedWidth == 0 ? requestedHeight : requestedWidth) / (requestedHeight == 0 ? requestedWidth : requestedHeight);
            float scale = 1f;

            if (requestedScale > maxWscale && ((maxWscale * requestedHeight) < maxHeight))
            {
                scale = maxWidth / (float)requestedWidth;
            }
            else
            {
                scale = maxHeight / (float)requestedHeight;
            }

            if (!allowUpscaling)
            {
                scale = Math.Min(scale, 1);
            }
            // TODO: make this configurable. spec recommends using maxWidth/maxHeight/maxArea with sizeAboveFull but you don't have to,
            // so still need some DoS protection!
            else
            {
                if (scale > 10)
                    scale = 1;
            }

            return (Convert.ToInt32(requestedWidth * scale), Convert.ToInt32(requestedHeight * scale), scale);

        }

        private static void CheckBounds(this ProcessState state)
        {
            if (state.RegionHeight == 0 || state.RegionWidth == 0)
            {
                throw new ArgumentException("Width or Height can not be 0");
            }

            if (state.StartX < 0 || state.StartY < 0)
            {
                throw new ArgumentException("X or Y must be unsigned");
            }
        }

        private static void CheckRequest(in this ImageRequest req, bool allowUpscaling)
        {
            if(!allowUpscaling && req.Size.Upscale)
            {
                throw new NotSupportedException("sizeUpscaling feature is not supported");
            }

            if (req.Region.Mode == ImageRegionMode.Region)
            {
                if (req.Region.Width == 0 || req.Region.Height == 0)
                {
                    throw new ArgumentException("Width or Height can not be 0");
                }
            }
            
            switch (req.Size.Mode)
            {
                case ImageSizeMode.Distort:
                    if (req.Size.Width == 0 || req.Size.Height == 0)
                    {
                        throw new ArgumentException("Width or Height can not be 0");
                    }
                    break;
                case ImageSizeMode.MaintainAspectRatio:
                    if (req.Size.Width == 0 && req.Size.Height == 0)
                    {
                        throw new ArgumentException("Width and Height can not both be 0");
                    }
                    break;
                case ImageSizeMode.PercentageScaled:
                    if (req.Size.Percent > 100.0 && !req.Size.Upscale)
                    {
                        throw new ArgumentException("The value of n must not be greater than 100. Use ^pct syntax if available.");
                    }
                    break;
                default:
                    break;
            }

        }

    }
}
