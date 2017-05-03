using TremendousIIIF.Common;
using System;
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
        public int MaxQualityLayers { get; set; }

        public Dictionary<string, string> OutputFormatQuality {  get; set; }
        

        public int GetOutputFormatQuality(ImageFormat format)
        {
            if(null == OutputFormatQuality)
            {
                return DefaultEncodingQuality;
            }
            OutputFormatQuality.TryGetValue(format.ToString(), out string qualityString);
            if(!Int32.TryParse(qualityString, out int quality))
            {
                return DefaultEncodingQuality;
            }
            return (quality == 0) ? DefaultEncodingQuality : quality;
        }
    }
}
