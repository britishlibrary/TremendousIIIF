using Image.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using TremendousIIIF.Common;

namespace TremendousIIIF.Validation
{
    public static class ImageRequestValidator
    {
        public static ImageRequest Validate(string identifier, string region, string size, string rotation, string quality, string format, int maxWidth, int maxHeight, int maxArea, List<ImageFormat> supportedFormats)
        {
            return new ImageRequest
            {
                ID = identifier,
                Region = CalculateRegion(region),
                Rotation = ParseRotation(rotation),
                Size = CalculateSize(size),
                Quality = ParseQuality(quality),
                Format = ParseFormat(format, supportedFormats),
                MaxArea = maxArea,
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };
        }

        private static ImageRotation ParseRotation(string rotation)
        {
            var degreesString = rotation.Replace("!", "");
            if (!int.TryParse(degreesString, out int degrees) || (degrees < 0 || degrees > 360))
            {
                throw new ArgumentException("Invalid number of degrees", "rotation");
            }
            return new ImageRotation
            {
                Degrees = degrees,
                Mirror = rotation.StartsWith("!")
            };
        }
        /// <summary>
        /// Validates requested format first against those supported by IIIF Image API 2.1, then against those <paramref name="supportedFormats"/> enabled in configuration
        /// </summary>
        /// <param name="formatString">The raw format string (jpg,png,webm,etc)</param>
        /// <param name="supportedFormats"></param>
        /// <returns></returns>
        public static ImageFormat ParseFormat(string formatString, List<ImageFormat> supportedFormats)
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

        public static ImageQuality ParseQuality(string qualityString)
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
            char[] Delimiter = new[] { ',' };
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
                    return new ImageRegion
                    {
                        Mode = regionMode,
                        X = Single.Parse(regions[0]),
                        Y = Single.Parse(regions[1]),
                        Width = Single.Parse(regions[2]),
                        Height = Single.Parse(regions[3])
                    };
                default:
                    return new ImageRegion { Mode = regionMode, X = 0f, Y = 0f, Width = 0f, Height = 0f };
            }
        }

        public static ImageSize CalculateSize(string size_string)
        {
            ImageSizeMode sizeMode;
            var percentage = 1f;
            int width = 0;
            int height = 0;
            switch (size_string.Substring(0, Math.Min(size_string.Length, 4)))
            {
                case "full":
                case "max":
                    sizeMode = ImageSizeMode.Max;
                    break;
                case "pct:":
                    sizeMode = ImageSizeMode.PercentageScaled;
                    size_string = size_string.Substring(4);
                    percentage = Convert.ToSingle(size_string) / 100;
                    break;
                default:
                    if (size_string.StartsWith("!"))
                    {

                        sizeMode = ImageSizeMode.MaintainAspectRatio;
                        size_string = size_string.Substring(1);
                    }
                    else if (size_string.Contains(","))
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
                    var sizes = size_string.Split(',');
                    if (sizes.Length != 2 || sizes.All(s => s.Length == 0))
                    {
                        throw new FormatException("Invald size format specified");
                    }
                    if (sizes[0] != string.Empty)
                    {
                        width = Int32.Parse(sizes[0]);
                    }
                    if (sizes[1] != string.Empty)
                    {
                        height = Int32.Parse(sizes[1]);
                    }
                    if (sizes.Where(s => s != string.Empty).Count() == 1)
                    {
                        sizeMode = ImageSizeMode.MaintainAspectRatio;
                    }
                    break;
            }

            return new ImageSize
            {
                Width = width,
                Height = height,
                Mode = sizeMode,
                Percent = percentage
            };
        }

    }
}