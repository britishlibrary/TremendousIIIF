namespace Image.Common
{
    public readonly struct ImageSize
    {
        public ImageSize(ImageSizeMode mode, float? percent, int width, int height, bool upscale)
        {
            Mode = mode;
            Percent = percent;
            Width = width;
            Height = height;
            Upscale = upscale;
        }
        public ImageSize(ImageSizeMode mode, float? percent, int width, int height)
        {
            Mode = mode;
            Percent = percent;
            Width = width;
            Height = height;
            Upscale = false;
        }
        public ImageSize(ImageSizeMode mode, float? percent)
        {
            Mode = mode;
            Percent = percent;
            Width = 0;
            Height = 0;
            Upscale = false;
        }
        public ImageSize(ImageSizeMode mode)
        {
            Mode = mode;
            Percent = 0;
            Width = 0;
            Height = 0;
            Upscale = false;
        }
        public ImageSizeMode Mode { get; }

        public float? Percent { get; }

        public int Width { get; }
        public int Height { get; }

        public bool Upscale { get; }

    }


}