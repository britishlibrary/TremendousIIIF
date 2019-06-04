using System.Collections.Generic;

namespace Image.Common
{
    public class Metadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int ScalingLevels { get; set; }
        public bool HasGeoData { get; set; }
        public int Qualities { get; set; }

        public IEnumerable<(int,int)> Sizes { get; set; }
    }
}
