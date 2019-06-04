using System.Collections.Generic;

namespace TremendousIIIF.Common.Configuration
{
    public class ImageQuality
    {
        public ImageQuality()
        {
            DefaultEncodingQuality = 80;
            OutputDpi = 600;
            WeightedRMSE = 0.004f;
            MaxQualityLayers = 0;
        }
        public int DefaultEncodingQuality { get; set; }
        public int OutputDpi { get; set; }
        public float WeightedRMSE { get; set; }
        /// <summary>
        /// Maximum number of quality layers to use when decoding a JP2. 
        /// If set to a negative number, it will be "Unlimited" and use all the layers available in the image. 
        /// If it is set to 0, it will use half the number of layers in the image.
        /// </summary>
        public int MaxQualityLayers { get; set; }

        public Dictionary<string, string> OutputFormatQuality { get; set; }

        public int GetOutputFormatQuality(ImageFormat format)
        {
            if (null == OutputFormatQuality)
            {
                return DefaultEncodingQuality;
            }
            OutputFormatQuality.TryGetValue(format.ToString(), out string qualityString);
            if (!int.TryParse(qualityString, out int quality))
            {
                return DefaultEncodingQuality;
            }
            return (quality == 0) ? DefaultEncodingQuality : quality;
        }
    }
}
