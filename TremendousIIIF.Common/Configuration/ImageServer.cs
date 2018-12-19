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
        }
        public ImageQuality ImageQuality { get; set; }

        public ApiVersion DefaultAPIVersion { get; set; }

        public string HealthcheckIdentifier { get; set; }
        public string Location { get; set; }
        public int DefaultTileWidth { get; set; }
        public bool AllowSizeAboveFull { get; set; }
        public int MaxArea { get; set; }
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

        /// <summary>
        /// <para>Image API 2.1 supports these formats - as we are complicant with level 2, we support jpg & png by default</para>
        /// Syntax Level 0 	Level 1 	Level 2
        /// jpg required    required required
        /// png optional    optional required
        /// tif optional    optional optional
        /// gif optional    optional optional
        /// pdf optional    optional optional
        /// jp2 optional    optional optional
        /// webp optional    optional optional
        /// <para>However, we also have support for others which can be controlled with configuration</para>
        /// <para>Not all are currently supported though!</para>
        /// </summary>
        private List<ImageFormat> RequiredFormats = new List<ImageFormat> { ImageFormat.jpg, ImageFormat.png };
        /// <summary>
        /// Blah
        /// </summary>
        public List<string> AdditionalOutputFormats { get; set; }

        public PdfMetadata PdfMetadata { get; set; }

        public List<ImageFormat> SupportedFormats()
        {
            return AdditionalOutputFormats
                .Select(f => { Enum.TryParse(f, out ImageFormat result); return result; })
                .Concat(RequiredFormats)
                .ToList();
        }

    }
}
