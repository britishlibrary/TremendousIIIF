using Image.Common;
using ImageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using TremendousIIIF.Common;

namespace TremendousIIIF.Validation
{
    public static class ImageRequestValidator
    {
        #region Regex
        private const string qualityFormatPattern = @"(?<quality>(native|color|grey|bitonal))(\.(?<format>(jpg|gif|png|jp2)))?";
        private static readonly Regex rQualityFormat = new Regex(qualityFormatPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const string regionPattern = @"full|square|" +
                        @"(pct:(?<px>\d+\.?\d*),(?<py>\d+\.?\d*),(?<pw>\d+\.?\d*),(?<ph>\d+\.?\d*))|" +
                        @"(?<x>\d+),(?<y>\d+),(?<w>\d+),(?<h>\d+)";

        private static readonly Regex rRegion = new Regex(regionPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const string sizePattern = @"(?<full>full|max)$|(?<width>\d+),(?<height>\d+)|(?<width>\d+),|,(?<height>\d+)|" +
                    @"(pct:(?<pct>\d+(\.\d+)?))|" +
                    @"(!?<exact>(?<width>\d+),(?<height>\d+))";
        private static readonly Regex rSize = new Regex(sizePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion

        public static ImageRequest Validate(string identifier, string region, string size, string rotation, string quality, string format, int maxWidth, int maxHeight, int maxArea)
        {

            return new ImageRequest
            {
                ID = identifier,
                Region = CalculateRegionCustom(region),
                Rotation = ParseRotation(rotation),
                Size = CalculateSizeCustom(size),
                Quality = ParseQuality(quality),
                Format = ParseFormat(format),
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

        public static ImageFormat ParseFormat(string formatString)
        {
            if (!Enum.TryParse(formatString, out ImageFormat format))
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
        public static ImageRegion CalculateRegionCustom(string region_string)
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
                    throw new NotImplementedException("square not supported");
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
        public static ImageRegion CalculateRegion(string region_string)
        {
            ImageRegionMode regionMode;
            Match regionMatch = rRegion.Match(region_string);
            switch (region_string.Substring(0, 4))
            {
                case "full":
                    regionMode = ImageRegionMode.Full;
                    break;
                case "squa":
                    regionMode = ImageRegionMode.Square;
                    throw new NotImplementedException("square not supported");
                case "pct:":
                    regionMode = ImageRegionMode.PercentageRegion;
                    break;
                default:
                    regionMode = ImageRegionMode.Region;
                    break;
            }

            Func<string, string, string, string, ImageRegion> blah = (x, y, w, h) =>
            {
                return new ImageRegion
                {
                    Mode = regionMode,
                    X = Single.Parse(x),
                    Y = Single.Parse(y),
                    Width = Single.Parse(w),
                    Height = Single.Parse(h)
                };
            };

            switch (regionMode)
            {
                case ImageRegionMode.PercentageRegion:
                    return blah(regionMatch.Groups["px"].Value, regionMatch.Groups["py"].Value, regionMatch.Groups["pw"].Value, regionMatch.Groups["ph"].Value);
                case ImageRegionMode.Region:
                    return blah(regionMatch.Groups["x"].Value, regionMatch.Groups["y"].Value, regionMatch.Groups["w"].Value, regionMatch.Groups["h"].Value);
                default:
                    return new ImageRegion { Mode = regionMode, X = 0f, Y = 0f, Width = 0f, Height = 0f };
            }

        }

        public static ImageSize CalculateSizeCustom(string size_string)
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

                        sizeMode = ImageSizeMode.SpecifiedFit;
                        size_string = size_string.Substring(1);
                    }
                    else if (size_string.Contains(","))
                    {
                        sizeMode = ImageSizeMode.Exact;
                    }
                    else
                    {
                        throw new FormatException("Invalid size format specified");
                    }
                    break;
            }

            switch (sizeMode)
            {

                case ImageSizeMode.SpecifiedFit:
                case ImageSizeMode.Exact:
                    var sizes = size_string.Split(',');
                    if (sizes.Length != 2 || sizes.All(s => s.Length == 0))
                    {
                        throw new FormatException("invald size format specified");
                    }
                    if (sizes[0] != string.Empty)
                    {
                        width = Int32.Parse(sizes[0]);
                    }
                    if (sizes[1] != string.Empty)
                    {
                        height = Int32.Parse(sizes[1]);
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

        public static ImageSize CalculateSize(string size_string)
        {

            Match m = rSize.Match(size_string);
            var mode = ImageSizeMode.Max;
            var percentage = 1f;
            int width = 0;
            int height = 0;

            if (m.Success)
            {
                if (m.Groups["full"].Success)
                {
                    mode = ImageSizeMode.Max;
                }
                else
                {
                    if (m.Groups["scale"].Success)
                    {
                        mode = ImageSizeMode.SpecifiedFit;
                    }
                    else if (m.Groups["pct"].Success)
                    {
                        mode = ImageSizeMode.PercentageScaled;
                        if (m.Groups["pct"].Success)
                        {
                            percentage = Convert.ToSingle(m.Groups["pct"].Value) / 100;
                        }
                    }
                    else
                    {
                        mode = ImageSizeMode.Exact;
                    }


                    if (mode != ImageSizeMode.PercentageScaled)
                    {
                        if (m.Groups["width"].Success)
                            width = Convert.ToInt32(m.Groups["width"].Value);

                        if (m.Groups["height"].Success)
                            height = Convert.ToInt32(m.Groups["height"].Value);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Size not correctly specified");
            }

            return new ImageSize
            {
                Width = width,
                Height = height,
                Mode = mode,
                Percent = percentage
            };
        }

    }
}