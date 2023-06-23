using System;
using System.Collections.Generic;
using System.Linq;

namespace TremendousIIIF.Common.Configuration
{
    public class ImageServer
    {
        private int _maxWidth;
        private int _maxHeight;
        public ImageServer()
        {
            AllowSizeAboveFull = false;
            MaxArea = int.MaxValue;
            MaxWidth = int.MaxValue;
            MaxHeight = int.MaxValue;
            DefaultTileWidth = 256;
            AdditionalOutputFormats = new List<string>();
            DefaultAPIVersion = ApiVersion.v2_1;
            AllowBitonal = false;
        }
        public ImageQuality ImageQuality { get; set; }
        /// <summary>
        /// If no suitable Accept header is supplied, this version of the IIIF Image API should be used
        /// </summary>
        public ApiVersion DefaultAPIVersion { get; set; }

        public string HealthcheckIdentifier { get; set; }
        public string Location { get; set; }
        public int DefaultTileWidth { get; set; }
        public bool AllowSizeAboveFull { get; set; }
        public int MaxArea { get; set; }
        /// <summary>
        /// Enable the GeoJson output in both the info.json response and the full outout in the geo.json response.
        /// </summary>
        public bool EnableGeoService { get; set; }
        /// <summary>
        /// Format string, to transform image identifier into externally resolveable address for geo data, e.g. https://api.bl.uk/image/iiif/ark:/81055/{0}/geo.json
        /// </summary>
        public string GeoDataBaseUri { get; set; }
        public string GeoDataPath { get; set; }
        /// <summary>
        /// For v3 of the Image API and above, bitonal is optional at level 2.
        /// </summary>
        public bool AllowBitonal { get; set; }
        /// <summary>
        /// Format string, to transform raw manifest ID into a Uri. e.g. https://api/bl.uk/metadata/iiif/{0}/manifest.json
        /// </summary>
        public string ManifestUriFormat { get; set; }
        public int MaxWidth
        {
            get
            {
                if (_maxWidth == int.MaxValue && _maxWidth != int.MaxValue)
                {
                    return MaxHeight;
                }
                return _maxWidth;
            }
            set
            {
                _maxWidth = value;
            }
        }
        public int MaxHeight
        {
            get
            {
                if (_maxHeight == int.MaxValue && _maxWidth != int.MaxValue)
                {
                    return MaxWidth;
                }
                return _maxHeight;
            }
            set
            {
                _maxHeight = value;
            }
        }
        public Uri BaseUri { get; set; }


        private readonly List<ImageFormat> RequiredFormats = new List<ImageFormat> { ImageFormat.jpg, ImageFormat.png };
        /// <summary>
        /// Blah
        /// </summary>
        public List<string> AdditionalOutputFormats { get; set; }

        public PdfMetadata PdfMetadata { get; set; }

        /// <summary>
        /// Image API 2.1 supports these formats - as we are complicant with level 2, we support jpg & png by default
        /// <list type="table">
        /// <listheader>
        /// <description>Syntax</description>
        /// <description>Level 0</description>
        /// <description>Level 1</description>
        /// <description>Level 2</description>
        /// </listheader>
        /// <item>
        /// <description>jpg</description>
        /// <description>required</description>
        /// <description>required</description>
        /// <description>required</description>
        /// </item>
        /// <item>
        /// <description>png</description>
        /// <description>optional</description>
        /// <description>optional</description>
        /// <description>required</description>
        /// </item>
        /// <item>
        /// <description>tif</description>
        /// <description>optional</description>
        /// <description>optional</description>
        /// <description>required</description>
        /// </item>
        /// <item>
        /// <description>gif</description>
        /// <description>optional</description>
        /// <description>optional</description>
        /// <description>required</description>
        /// </item>
        /// <item>
        /// <description>pdf</description>
        /// <description>optional</description>
        /// <description>optional</description>
        /// <description>required</description>
        /// </item>
        /// <item>
        /// <description>jp2</description>
        /// <description>optional</description>
        /// <description>optional</description>
        /// <description>required</description>
        /// </item>
        /// <item>
        /// <description>webp</description>
        /// <description>optional</description>
        /// <description>optional</description>
        /// <description>required</description>
        /// </item>
        /// </list>
        /// </summary>
        public List<ImageFormat> SupportedFormats()
        {
            return RequiredFormats
                .Concat(AdditionalOutputFormats.Select(f => { Enum.TryParse(f, out ImageFormat result); return result; }))
                .ToList();
        }
    }
}
