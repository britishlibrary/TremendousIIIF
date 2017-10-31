using System;
using Dimensions = System.ValueTuple<int, int, float>;

namespace Image.Common
{
    public static class ImageRequestInterpreter
    {
        static public ProcessState GetInterpretedValues(ImageRequest request, int originalWidth, int originalHeight, bool allowSizeAboveFull)
        {
            request.CheckRequest();
            ProcessState state = new ProcessState(request.ID);

            state.StartX = state.StartY = state.TileHeight = state.TileWidth = 0;
            state.OutputScale = state.ImageScale = 1;
            //state.OutputScale = request.Size.Percent.Value > maxWscale || request.Size.Percent.Value > maxHscale ? Math.Min(maxWscale, maxHscale) : request.Size.Percent.Value;
            switch (request.Region.Mode)
            {
                case ImageRegionMode.PercentageRegion:
                    state.StartX = Convert.ToInt32(request.Region.X / 100 * originalWidth);
                    state.StartY = Convert.ToInt32(request.Region.Y / 100 * originalHeight);
                    state.Width = state.TileWidth = Convert.ToInt32(request.Region.Width / 100 * originalWidth);
                    state.Height = state.TileHeight = Convert.ToInt32(request.Region.Height / 100 * originalHeight);
                    //wScale = (float)state.TileWidth / originalWidth;
                    //hScale = (float)state.TileHeight / originalHeight;
                    break;
                case ImageRegionMode.Region:
                    state.StartX = Convert.ToInt32(request.Region.X);
                    state.StartY = Convert.ToInt32(request.Region.Y);
                    state.Width = state.TileWidth = Convert.ToInt32(request.Region.Width);
                    state.Height = state.TileHeight = Convert.ToInt32(request.Region.Height);
                    //wScale = request.Size.Width / (float)state.TileWidth;
                    //hScale = request.Size.Height / (float)state.TileHeight;
                    break;
                case ImageRegionMode.Square:
                    state.Width = state.Height = state.TileHeight = state.TileWidth = Math.Min(originalWidth, originalHeight);

                    // pick the middle of the image
                    state.StartX = state.TileWidth == originalWidth ? 0 : Convert.ToInt32(Math.Round((originalWidth - originalHeight) / 2f));
                    state.StartY = state.TileHeight == originalHeight ? 0 : Convert.ToInt32(Math.Round((originalHeight - originalWidth) / 2f));

                    break;
                default:
                    state.Width = state.TileWidth = originalWidth;
                    state.Height = state.TileHeight = originalHeight;
                    break;
            }

            CheckBounds(state);
            #region old
            // we can simplify this
            //switch (request.Size.Mode)
            //{
            //    //case ImageSizeMode.Max:
            //    //case ImageSizeMode.PercentageScaled:
            //    case ImageSizeMode.Exact:
            //        var w = request.Size.Width == 0 ? originalWidth : (int)request.Size.Width;
            //        var h = request.Size.Height == 0 ? originalHeight : (int)request.Size.Height;
            //        (state.Width, state.Height, state.OutputScale) = (state.TileWidth, state.TileHeight, state.OutputScale) = ScaleOutput(request.MaxWidth, request.MaxHeight, w, h, allowSizeAboveFull);
            //        break;

            //    default:
            //        //state.OutputScale = request.Size.Percent.Value > maxWscale || request.Size.Percent.Value > maxHscale ? Math.Min(maxWscale, maxHscale) : request.Size.Percent.Value;
            //        //scalex = scaley = state.OutputScale;// * request.Size.Percent.Value;
            //        if (request.Region.Mode == ImageRegionMode.Full)
            //        {
            //            (state.Width, state.Height, state.OutputScale) = (state.TileWidth, state.TileHeight, state.OutputScale) = ScaleOutput(request.MaxWidth, request.MaxHeight, originalWidth, originalHeight, allowSizeAboveFull);
            //            //state.Width = state.TileHeight = Convert.ToInt32(Math.Round(originalHeight * state.OutputScale));
            //            //state.Height = state.TileWidth = Convert.ToInt32(Math.Round(originalWidth * state.OutputScale));

            //        }
            //        //else if (request.Region.Mode == ImageRegionMode.PercentageRegion)
            //        //{
            //        //    (state.Width, state.Height, state.OutputScale) = (state.TileWidth, state.TileHeight, state.OutputScale) = ScaleOutput(request.MaxWidth, request.MaxHeight, state.TileWidth, state.TileHeight, allowSizeAboveFull);
            //        //    //scalex = scaley = state.OutputScale;
            //        //    //state.Height = state.TileHeight = Convert.ToInt32(state.TileHeight * state.OutputScale);
            //        //    //state.Width = state.TileWidth = Convert.ToInt32(state.TileWidth * state.OutputScale);
            //        //}
            //        else
            //        {
            //            (state.Width, state.Height, state.OutputScale) = (state.TileWidth, state.TileHeight, state.OutputScale) = ScaleOutput(request.MaxWidth, request.MaxHeight, state.TileWidth, state.TileHeight, allowSizeAboveFull);
            //            //state.Height = state.TileHeight = Convert.ToInt32(state.TileHeight * state.OutputScale);
            //            //state.Width = state.TileWidth = Convert.ToInt32(state.TileWidth * state.OutputScale);
            //        }

            //        break;
            //    // The width and height of the returned image are exactly w and h. The aspect ratio of the returned image may be different than the extracted region, resulting in a distorted image.
            //    //case ImageSizeMode.Exact:
            //    //    if (request.Region.Mode == ImageRegionMode.Square)
            //    //    {
            //    //        var w = request.Size.Width == 0 ? request.Size.Height : Math.Min(request.Size.Width, request.Size.Height);
            //    //        var h= request.Size.Height == 0 ? request.Size.Width : Math.Min(request.Size.Width, request.Size.Height);
            //    //        (state.Width, state.Height, state.OutputScale) = ScaleOutput(request.MaxWidth, request.MaxHeight, w, h, allowSizeAboveFull);

            //    //    }
            //    //    else if (request.Region.Mode == ImageRegionMode.Full)
            //    //    {
            //    //        //scalex = wScale;
            //    //        //scaley = hScale;
            //    //        //if (request.Size.Width == 0 || request.Size.Height == 0)
            //    //        //{
            //    //        //    if (scalex > scaley)
            //    //        //    {
            //    //        //        state.OutputScale = scalex;
            //    //        //    }
            //    //        //    else
            //    //        //    {
            //    //        //        state.OutputScale = scaley;
            //    //        //    }

            //    //        //    state.Width = state.TileWidth = Convert.ToInt32(originalWidth * state.OutputScale);
            //    //        //    state.Height = state.TileHeight = Convert.ToInt32(originalHeight * state.OutputScale);
            //    //        //}
            //    //        //else
            //    //        //{
            //    //        //    state.Width = state.TileWidth = Convert.ToInt32(originalWidth * scalex);
            //    //        //    state.Height = state.TileHeight = Convert.ToInt32(originalHeight * scaley);
            //    //        //}
            //    //        (state.Width, state.Height, state.OutputScale) = (state.TileWidth, state.TileHeight, state.OutputScale) = ScaleOutput(request.MaxWidth, request.MaxHeight, originalWidth, originalHeight, allowSizeAboveFull);
            //    //    }
            //        //else if (request.Size.Width != 0 && request.Size.Height != 0)
            //        //{
            //        //    if (state.TileWidth > 0)
            //        //        scalex = (float)request.Size.Width / state.TileWidth;
            //        //    else
            //        //        scalex = wScale;

            //        //    if (state.TileHeight > 0)
            //        //        scaley = (float)request.Size.Height / state.TileHeight;
            //        //    else
            //        //        scaley = hScale;

            //        //    if (request.Size.Width == request.Size.Height)
            //        //    {
            //        //        state.OutputScale = scalex; // both scales should be the same so shouldn't matter
            //        //    }
            //        //    else if (request.Size.Width > request.Size.Height)
            //        //    {
            //        //        state.OutputScale = scalex;
            //        //    }
            //        //    else
            //        //    {
            //        //        state.OutputScale = scaley;
            //        //    }
            //        //    state.Width = request.Size.Width;
            //        //    state.Height = request.Size.Height;
            //        //}
            //        //else if (request.Size.Width > request.Size.Height)
            //        //{
            //        //    state.Width = request.Size.Width;
            //        //    state.Height = request.Size.Height;

            //        //    if (state.TileWidth > 0)
            //        //    {
            //        //        state.OutputScale = (float)request.Size.Width / state.TileWidth;
            //        //        state.Height = Convert.ToInt32(state.TileHeight * state.OutputScale);
            //        //    }
            //        //    else
            //        //    {
            //        //        state.OutputScale = wScale;
            //        //        state.Width = Convert.ToInt32(state.TileWidth * state.OutputScale);
            //        //    }

            //        //    scaley = scalex = state.OutputScale;


            //        //}
            //        //else
            //        //{
            //        //    if (state.TileHeight > 0)
            //        //        state.OutputScale = (float)request.Size.Height / state.TileHeight;
            //        //    else
            //        //        state.OutputScale = hScale;

            //        //    scaley = scalex = state.OutputScale;
            //        //}

            //        //if (wScale != 0 && hScale != 0)
            //        //{
            //        //    if (wScale < hScale)
            //        //        state.OutputScale = wScale;
            //        //    else
            //        //        state.OutputScale = hScale;
            //        //}

            //        //// get all 
            //        //if (request.Size.Mode != ImageSizeMode.Exact && request.Region.Mode == ImageRegionMode.Full)
            //        //{
            //        //    if (request.Size.Width != 0)
            //        //        state.TileWidth = request.Size.Width;
            //        //    if (request.Size.Height != 0)
            //        //        state.TileHeight = request.Size.Height;
            //        //}

            //        break;
            //    //case ImageSizeMode.SpecifiedFit:
            //        //if (request.Region.Mode == ImageRegionMode.Square)
            //        //{
            //        //    state.Width = request.Size.Width == 0 ? request.Size.Height : request.Size.Width;
            //        //    state.Height = request.Size.Height == 0 ? request.Size.Width : request.Size.Height;
            //        //}
            //        //if (state.TileWidth > 0)
            //        //    scalex = (float)request.Size.Width / state.TileWidth;
            //        //else
            //        //    scalex = wScale;

            //        //if (state.TileHeight > 0)
            //        //    scaley = (float)request.Size.Height / state.TileHeight;
            //        //else
            //        //    scaley = hScale;

            //        //if (scalex < scaley)
            //        //    state.OutputScale = scalex;
            //        //else
            //        //    state.OutputScale = scaley;

            //        //if (request.Region.Mode == ImageRegionMode.Full)
            //        //{
            //        //    state.TileHeight = Convert.ToInt32(Math.Round(originalHeight * state.OutputScale).ToString());
            //        //    state.TileWidth = Convert.ToInt32(Math.Round(originalWidth * state.OutputScale).ToString());
            //        //}

            //        //break;
            //}

            //if (!(request.Region.Mode == ImageRegionMode.Full || (request.Region.Mode == ImageRegionMode.PercentageRegion && request.Region.Width == 100)))
            //{
            //    if (scalex < scaley)
            //        state.ImageScale = scalex;
            //    else
            //        state.ImageScale = scaley;
            //}
            #endregion

            switch (request.Size.Mode)
            {
                case ImageSizeMode.Max:
                    state.Width = originalWidth;
                    state.Height = originalHeight;
                    break;
                case ImageSizeMode.PercentageScaled:
                    state.Width = Convert.ToInt32(state.TileWidth * request.Size.Percent.Value);
                    state.Height = Convert.ToInt32(state.TileHeight * request.Size.Percent.Value);
                    state.OutputScale = request.Size.Percent.Value;
                    break;
                case ImageSizeMode.Distort:
                    float scaledx, scaledy = 1f;
                    
                        scaledy = request.Size.Height / (float)state.TileHeight;
                        scaledx = request.Size.Width / (float)state.TileWidth;

                    state.Width = Convert.ToInt32(state.TileWidth * scaledx);
                    state.Height = Convert.ToInt32(state.TileHeight * scaledy);
                    if (scaledx < scaledy)
                    {
                        state.OutputScale = scaledx;
                    }
                    else
                    {
                        state.OutputScale = scaledy;
                    }
                    break;
                case ImageSizeMode.MaintainAspectRatio:
                    if (request.Size.Width != 0 && request.Size.Height == 0)
                    {
                        var scale = request.Size.Width / (float)state.TileWidth;
                        state.Width = Convert.ToInt32(state.TileWidth * scale);
                        state.Height = Convert.ToInt32(state.TileHeight * scale);
                        //if(request.Region.Mode == ImageRegionMode.Full)
                        state.OutputScale = scale;
                    }
                    else if (request.Size.Width == 0 && request.Size.Height != 0)
                    {
                        var scale = request.Size.Height / (float)state.TileHeight;
                        state.Width = Convert.ToInt32(state.TileWidth * scale);
                        state.Height = Convert.ToInt32(state.TileHeight * scale);
                        state.OutputScale = scale;
                    }
                    else
                    {
                        var scalex = request.Size.Height / (float)state.TileHeight;
                        var scaley = request.Size.Width / (float)state.TileWidth;

                        state.Width = Convert.ToInt32(state.TileWidth * scalex);
                        state.Height = Convert.ToInt32(state.TileHeight * scaley);
                        if (scalex < scaley)
                            state.OutputScale = scalex;
                        else
                            state.OutputScale = scaley;
                    }
                    break;
            }

            // final bounds reduction
            float max_scale = 1f;
            (state.Width, state.Height, max_scale)
                = ScaleOutput(request.MaxWidth, request.MaxHeight, state.Width, state.Height, allowSizeAboveFull);
            state.ImageScale = Math.Min(max_scale, state.OutputScale);

            //if (request.Region.Mode != ImageRegionMode.Full)
            //{
            //    state.ImageScale = max_scale;
            //}

                state.CheckBounds();

            return state;
        }

        private static Dimensions ScaleOutput(int maxWidth, int maxHeight, int requestedWidth, int requestedHeight, bool allowSizeAboveFull)
        {

            float maxWscale = maxWidth == int.MaxValue ? 1 : (float)maxWidth / requestedWidth;
            float maxHscale = maxHeight == int.MaxValue ? 1 : (float)maxHeight / requestedHeight;

            var requestedScale = (requestedWidth == 0 ? requestedHeight : requestedWidth) / (requestedHeight == 0 ? requestedWidth : requestedHeight);
            float scale = 1f;

            if (requestedScale > maxWscale)
            {
                scale = maxWidth / (float)requestedWidth;
            }
            else
            {
                scale = maxHeight / (float)requestedHeight;
            }

            if (!allowSizeAboveFull)
            {
                scale = Math.Min(scale, 1);
            }
            else
            {
                if (scale > 10)
                    scale = 1;
            }

            return (Convert.ToInt32(requestedWidth * scale), Convert.ToInt32(requestedHeight * scale), scale);

        }

        private static void CheckBounds(this ProcessState state)
        {
            if (state.TileHeight == 0 || state.TileWidth == 0)
            {
                throw new ArgumentException("Width or Height can not be 0");
            }

            if (state.StartX < 0 || state.StartY < 0)
            {
                throw new ArgumentException("X or Y must be unsigned");
            }
        }

        private static void CheckRequest(this ImageRequest req)
        {
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

                    break;
                default:
                    break;
            }

        }

    }
}
