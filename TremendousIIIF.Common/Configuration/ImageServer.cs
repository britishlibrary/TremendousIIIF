using System;

namespace TremendousIIIF.Common.Configuration
{
    public class ImageServer
    {
        public ImageServer()
        {
            AllowSizeAboveFull = false;
            MaxArea = int.MaxValue;
            MaxWidth = int.MaxValue;
            MaxHeight = int.MaxValue;
            DefaultTileWidth = 256;
        }
        public ImageQuality ImageQuality { get; set; }

        public string HealthcheckIdentifier { get; set; }
        public string Location { get; set; }
        public int DefaultTileWidth { get; set; }
        public bool AllowSizeAboveFull { get; set; }
        public int MaxArea { get; set; }
        public int MaxWidth { get; set; }
        public int MaxHeight { get; set; }
        public Uri BaseUri { get; set; }
    }
}
