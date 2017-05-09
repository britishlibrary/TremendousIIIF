using System;
using System.Linq;

namespace Image.Common
{
    public static class ImageRequestInterpreter
    {
        static public ProcessState GetInterpretedValues(ImageRequest request, int originalWidth, int originalHeight, bool allowSizeAboveFull)
        {
            // TODO: deal with square region!

            bool maxUsed = false;
            float scalex, scaley, wScale, hScale;

            float requestedWscale = (float)request.Size.Width / originalWidth;
            float requestedHscale = (float)request.Size.Height / originalHeight;
            float maxWscale = (float)request.MaxWidth / originalWidth;
            float maxHscale = (float)request.MaxHeight / originalHeight;


            wScale = allowSizeAboveFull ? Math.Min(requestedWscale, maxWscale) : Math.Min(requestedWscale, maxWscale);
            hScale = allowSizeAboveFull ? Math.Min(requestedHscale, maxHscale) : Math.Min(requestedHscale, maxHscale);

            if (!allowSizeAboveFull && request.Size.Percent.HasValue)
            {
                request.Size.Percent = Math.Min(request.Size.Percent.Value, 1);
            }

            ProcessState state = new ProcessState(request.ID);

            state.StartX = state.StartY = state.TileHeight = state.TileWidth = 0;
            state.OutputScale = state.ImageScale = scalex = scaley = 1;

            switch (request.Region.Mode)
            {
                case ImageRegionMode.PercentageRegion:
                    state.StartX = Convert.ToInt32(request.Region.X / 100 * originalWidth);
                    state.StartY = Convert.ToInt32(request.Region.Y / 100 * originalHeight);
                    state.TileWidth = Convert.ToInt32(request.Region.Width / 100 * originalWidth);
                    state.TileHeight = Convert.ToInt32(request.Region.Height / 100 * originalHeight);
                    wScale = request.Size.Width / (float)state.TileWidth;
                    hScale = request.Size.Height / (float)state.TileHeight;
                    break;
                case ImageRegionMode.Region:
                    state.StartX = Convert.ToInt32(request.Region.X);
                    state.StartY = Convert.ToInt32(request.Region.Y);
                    state.TileWidth = Convert.ToInt32(request.Region.Width);
                    state.TileHeight = Convert.ToInt32(request.Region.Height);
                    wScale = request.Size.Width / (float)state.TileWidth;
                    hScale = request.Size.Height / (float)state.TileHeight;
                    break;
            }
            // we can simplify this
            switch (request.Size.Mode)
            {
                case ImageSizeMode.Max:
                case ImageSizeMode.PercentageScaled:
                    state.OutputScale = request.Size.Percent.Value;
                    scalex = scaley = state.OutputScale * request.Size.Percent.Value;
                    if (request.Region.Mode == ImageRegionMode.Full)
                    {
                        state.Width = state.TileHeight = Convert.ToInt32(Math.Round(originalHeight * state.OutputScale));
                        state.Height = state.TileWidth = Convert.ToInt32(Math.Round(originalWidth * state.OutputScale));
                        
                    }
                    else if (request.Region.Mode == ImageRegionMode.PercentageRegion)
                    {
                        scalex = scaley = state.OutputScale;
                        state.Width = state.TileWidth;
                        state.Height = state.TileHeight;
                    }
                    else
                    {
                        state.Height = state.TileHeight = Convert.ToInt32(state.TileHeight * state.OutputScale);
                        state.Width = state.TileWidth = Convert.ToInt32(state.TileWidth * state.OutputScale);
                    }

                    break;
                case ImageSizeMode.Exact:
                    if (request.Region.Mode == ImageRegionMode.Full)
                    {
                        scalex = wScale;
                        scaley = hScale;
                        if (request.Size.Width == 0 || request.Size.Height == 0)
                        {
                            if (scalex > scaley)
                            {
                                state.OutputScale = scalex;
                            }
                            else
                            {
                                state.OutputScale = scaley;
                            }

                            state.Width = state.TileWidth = Convert.ToInt32(originalWidth * state.OutputScale);
                            state.Height = state.TileHeight = Convert.ToInt32(originalHeight * state.OutputScale);
                        }
                        else
                        {
                            state.Width = state.TileWidth = Convert.ToInt32(originalWidth * scalex);
                            state.Height = state.TileHeight = Convert.ToInt32(originalHeight * scaley);
                        }
                    }
                    else if (request.Size.Width != 0 && request.Size.Height != 0)
                    {
                        if (state.TileWidth > 0)
                            scalex = (float)request.Size.Width / state.TileWidth;
                        else
                            scalex = wScale;

                        if (state.TileHeight > 0)
                            scaley = (float)request.Size.Height / state.TileHeight;
                        else
                            scaley = hScale;

                        if (request.Size.Width == request.Size.Height)
                        {
                            state.OutputScale = scalex; // both scales should be the same so shouldn't matter
                        }
                        else if (request.Size.Width > request.Size.Height)
                        {
                            state.OutputScale = scalex;
                        }
                        else
                        {
                            state.OutputScale = scaley;
                        }
                        state.Width = request.Size.Width;
                        state.Height = request.Size.Height;
                    }
                    else if (request.Size.Width > request.Size.Height)
                    {
                        state.Width = request.Size.Width;
                        state.Height = request.Size.Height;

                        if (state.TileWidth > 0)
                        {
                            state.OutputScale = (float)request.Size.Width / state.TileWidth;
                            state.Height = Convert.ToInt32(state.TileHeight * state.OutputScale);
                        }
                        else
                        {
                            state.OutputScale = wScale;
                            state.Width = Convert.ToInt32(state.TileWidth * state.OutputScale);
                        }

                        scaley = scalex = state.OutputScale;

                        
                    }
                    else
                    {
                        if (state.TileHeight > 0)
                            state.OutputScale = (float)request.Size.Height / state.TileHeight;
                        else
                            state.OutputScale = hScale;

                        scaley = scalex = state.OutputScale;
                    }

                    if (wScale != 0 && hScale != 0)
                    {
                        if (wScale < hScale)
                            state.OutputScale = wScale;
                        else
                            state.OutputScale = hScale;
                    }

                    // get all 
                    if (request.Size.Mode != ImageSizeMode.Exact && request.Region.Mode == ImageRegionMode.Full)
                    {
                        if (request.Size.Width != 0)
                            state.TileWidth = request.Size.Width;
                        if (request.Size.Height != 0)
                            state.TileHeight = request.Size.Height;
                    }

                    break;
                case ImageSizeMode.SpecifiedFit:
                    if (state.TileWidth > 0)
                        scalex = (float)request.Size.Width / state.TileWidth;
                    else
                        scalex = wScale;

                    if (state.TileHeight > 0)
                        scaley = (float)request.Size.Height / state.TileHeight;
                    else
                        scaley = hScale;

                    if (scalex < scaley)
                        state.OutputScale = scalex;
                    else
                        state.OutputScale = scaley;

                    if (request.Region.Mode == ImageRegionMode.Full)
                    {
                        state.TileHeight = Convert.ToInt32(Math.Round(originalHeight * state.OutputScale).ToString());
                        state.TileWidth = Convert.ToInt32(Math.Round(originalWidth * state.OutputScale).ToString());
                    }
                    break;
            }

            if (request.Region.Mode != ImageRegionMode.Full)
            {
                if (scalex < scaley)
                    state.ImageScale = scalex;
                else
                    state.ImageScale = scaley;
            }

            state.MaxUsed = maxUsed;

            CheckBounds(state);

            return state;
        }

        private static void CheckBounds(ProcessState state)
        {
            if (state.TileHeight == 0 || state.TileWidth == 0)
            {
                throw new ArgumentException("Width or Height can not be 0");
            }

            if (state.StartX < 0 || state.StartY < 0)
            {
                throw new ArgumentException("X or Y can not be 0");
            }
        }

    }
}
