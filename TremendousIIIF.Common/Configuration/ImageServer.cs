using System;
using System.Collections.Generic;
using System.Linq;

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
            AdditionalOutputFormats = new List<string>();
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
        // Image API 2.1 supports these formats - as we are complicant with level 2
        // we support jpg & png.
        // Syntax 	Level 0 	Level 1 	Level 2
        // jpg      required    required    required
        // png      optional    optional    required
        // tif      optional    optional    optional
        // gif      optional    optional    optional
        // pdf      optional    optional    optional
        // jp2      optional    optional    optional
        // webp     optional    optional    optional
        //
        // However, we also have support for others which can be controlled with configuration
        // Not all are currently supported though!
        private List<ImageFormat> RequiredFormats = new List<ImageFormat> { ImageFormat.jpg, ImageFormat.png };
        public List<string> AdditionalOutputFormats { get; set; }

        public List<ImageFormat> SupportedFormats()
        {
            return AdditionalOutputFormats
                .Select(f => { Enum.TryParse(f, out ImageFormat result); return result; })
                .Concat(RequiredFormats)
                .ToList();
        }

    }
}
