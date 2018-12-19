using Image.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using TremendousIIIF.Common;


namespace TremendousIIIF.Validation
{
    public static class ImageRequestValidator
    {
        static readonly char[] Delimiter = { ',' };
        public static ImageRequest Validate(string region, string size, string rotation, string quality, string format, string requestId, int maxWidth, int maxHeight, int maxArea, List<ImageFormat> supportedFormats)
        {
            return new ImageRequest(requestId, 
                                    CalculateRegion(region), 
                                    CalculateSize(size), 
                                    ParseRotation(rotation), 
                                    ParseQuality(quality), 
                                    ParseFormat(format, supportedFormats), 
                                    maxWidth, 
                                    maxHeight, 
                                    maxArea);
        }

        private static ImageRotation ParseRotation(in string rotation)
        {
            var degreesString = rotation.Replace("!", "");
            if (!int.TryParse(degreesString, out int degrees) || (degrees < 0 || degrees > 360))
            {
                throw new ArgumentException("Invalid number of degrees", "rotation");
            }
            return new ImageRotation(degrees, rotation.StartsWith("!"));
        }
        /// <summary>
        /// Validates requested format first against those supported by IIIF Image API 2.1, then against those <paramref name="supportedFormats"/> enabled in configuration
        /// </summary>
        /// <param name="formatString">The raw format string (jpg,png,webm,etc)</param>
        /// <param name="supportedFormats"></param>
        /// <returns></returns>
        public static ImageFormat ParseFormat(in string formatString, List<ImageFormat> supportedFormats)
        {
            // first check it's permitted by the Image API specification
            if (!Enum.TryParse(formatString, out ImageFormat format))
            {
                throw new ArgumentException("Unsupported format", "format");
            }
            // then check we either support it at our compliance level, or that we have explicitly enabled support for it
            if (!supportedFormats.Contains(format))
            {
                throw new ArgumentException("Unsupported format", "format");
            }

            return format;
        }

        public static ImageQuality ParseQuality(in string qualityString)
        {
            if (!Enum.TryParse(qualityString, out ImageQuality quality))
            {
                throw new ArgumentException("Unsupported format", "quality");
            }
            return quality;
        }

        public static void InvalidRegion()
        {
            throw new FormatException("Invalid region parameter");
        }
        public static ImageRegion CalculateRegion(string region_string)
        {
            ImageRegionMode regionMode;
            switch (region_string.Substring(0, 4))
            {
                case "full":
                    if (region_string != "full")
                        InvalidRegion();
                    regionMode = ImageRegionMode.Full;
                    break;
                case "squa":
                    if (region_string != "square")
                        InvalidRegion();
                    regionMode = ImageRegionMode.Square;
                    break;
                case "pct:":
                    regionMode = ImageRegionMode.PercentageRegion;
                    region_string = region_string.Substring(4);
                    break;
                default:
                    regionMode = ImageRegionMode.Region;
                    break;
            }

            switch (regionMode)
            {
                case ImageRegionMode.PercentageRegion:
                case ImageRegionMode.Region:
                    var regions = region_string.Split(Delimiter);
                    if (regions.Length != 4 || regions.Any(r => r.Length < 1))
                    {
                        InvalidRegion();
                    }
                    return new ImageRegion(regionMode, Single.Parse(regions[0]), Single.Parse(regions[1]), Single.Parse(regions[2]), Single.Parse(regions[3]));
                default:
                    return new ImageRegion(regionMode, 0f, 0f, 0f, 0f);
            }
        }

        public static ImageSize CalculateSize(string size_string)
        {
            ImageSizeMode sizeMode;
            var percentage = 1f;
            int width = 0;
            int height = 0;

            var size_span = size_string.AsSpan();

            var upscaling = size_span[0] == '^';
            var maintain_ar = size_span[upscaling?1:0] == '!';

            var modeStart = upscaling && maintain_ar ? 2: (upscaling || maintain_ar) ? 1 : 0;
            
            var sizeStart = modeStart;

            var mode = size_span.Slice(modeStart, Math.Min(size_span.Length - modeStart, 4));
            switch (mode)
            {
                // Ugh. feels like this should be a special case
                // https://github.com/dotnet/csharplang/issues/1881
                case var _ when mode.SequenceEqual("full".AsSpan()):
                case var _ when mode.SequenceEqual("max".AsSpan()):
                    //case "full":
                    //case "max":
                    sizeMode = ImageSizeMode.Max;
                    break;
                //case "pct:":
                case var _ when mode.SequenceEqual("pct:".AsSpan()):
                    sizeMode = ImageSizeMode.PercentageScaled;
                    // TODO: framework is rubbish compared to core :(
                    percentage = float.Parse(size_span.Slice(modeStart + 4).ToString()) / 100;
                    sizeStart += 4;
                    break;
                default:
                    if (maintain_ar)
                    {
                        sizeMode = ImageSizeMode.MaintainAspectRatio;
                    }
                    else if (mode.IndexOf(',') >= 0)
                    {
                        sizeMode = ImageSizeMode.Distort;
                    }
                    else
                    {
                        throw new FormatException("Invalid size format specified");
                    }
                    break;
            }

            switch (sizeMode)
            {
                case ImageSizeMode.MaintainAspectRatio:
                case ImageSizeMode.Distort:
                    var sizeSpan = size_span.Slice(sizeStart);
                    var commaPos = sizeSpan.IndexOf(',');
                    var first = sizeSpan.Slice(0, commaPos);
                    var second = sizeSpan.Slice(commaPos+1);
                    if(second.IsEmpty && first.IsEmpty)
                    {
                        throw new FormatException("Invald size format specified");
                    }
                    if (!first.IsEmpty)
                    {
                        width = int.Parse(first.ToString());
                    }
                    if (!second.IsEmpty)
                    {
                        height = int.Parse(second.ToString());
                    }
                    if ((first.IsEmpty && !second.IsEmpty) || (!first.IsEmpty && second.IsEmpty))
                    {
                        sizeMode = ImageSizeMode.MaintainAspectRatio;
                    }
                    break;
            }

            return new ImageSize(sizeMode, percentage, width, height, upscaling);

        }

    }
}