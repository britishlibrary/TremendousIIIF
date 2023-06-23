namespace Image.Common
{
    public readonly struct Metadata
    {
        public Metadata(int width, int height, int tileWidth, int tileHeight, int scalingLevels, bool hasGeoData, int qualities, IEnumerable<(int, int)> sizes)
        {
            Width = width;
            Height = height;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            ScalingLevels = scalingLevels;
            HasGeoData = hasGeoData;
            Qualities = qualities;
            Sizes = sizes;
        }
        public int Width { get; }
        public int Height { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public int ScalingLevels { get; }
        public bool HasGeoData { get; }
        public int Qualities { get; }

        public IEnumerable<(int, int)> Sizes { get; }
    }
}
