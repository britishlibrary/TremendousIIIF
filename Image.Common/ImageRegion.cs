namespace Image.Common
{
    public readonly struct ImageRegion
    {
        public ImageRegion(ImageRegionMode mode, float x, float y, float width, float height)
        {
            Mode = mode;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        public ImageRegion(ImageRegionMode mode)
        {
            Mode = mode;
            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
        }
        public ImageRegionMode Mode { get; }
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
    }
}
