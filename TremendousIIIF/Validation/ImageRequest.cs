using Image.Common;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using TremendousIIIF.Common;

namespace TremendousIIIF.Validation
{
    public static class ImageRequestValidator
    {
        static readonly char[] Delimiter = { ',' };

        public static Either<ValidationError, ImageRequest> Validate(string region, string size, string rotation, string quality, string format, int maxWidth, int maxHeight, int maxArea, List<ImageFormat> supportedFormats, ApiVersion apiVersion = ApiVersion.v3_0)
        {

            return
                from _region in CalculateRegion(region).ToEither(() => new ValidationError("Invalid region value", nameof(region)))
                from _size in CalculateSize(size, apiVersion).ToEither(() => new ValidationError("Invalid size value", nameof(size)))
                from _rotation in ParseRotation(rotation).ToEither(() => new ValidationError("Invalid rotation value", nameof(rotation)))
                from _quality in ParseQuality(quality).ToEither(() => new ValidationError("Invalid quality value", nameof(quality)))
                from _format in ParseFormat(format, supportedFormats)
                select new ImageRequest(_region, _size, _rotation, _quality, _format, maxWidth, maxHeight, maxArea);
        }

        private static Option<ImageRotation> ParseRotation(in string rotation)
        {
            var degreesString = rotation.Replace("!", "");
            if (!int.TryParse(degreesString, out int degrees) || (degrees < 0 || degrees > 360))
            {
                return Option<ImageRotation>.None;
            }
            return new ImageRotation(degrees, rotation.StartsWith("!"));
        }
        /// <summary>
        /// Validates requested format first against those supported by IIIF Image API 2.1, then against those <paramref name="supportedFormats"/> enabled in configuration
        /// </summary>
        /// <param name="formatString">The raw format string (jpg,png,webm,etc)</param>
        /// <param name="supportedFormats"></param>
        /// <returns></returns>
        public static Either<ValidationError, ImageFormat> ParseFormat(in string formatString, List<ImageFormat> supportedFormats)
        {
            // first check it's permitted by the Image API specification
            if (!Enum.TryParse(formatString, out ImageFormat format))
            {
                return new ValidationError("The requested format is not supported by the IIIF Image API specification", nameof(format));
            }
            // then check we either support it at our compliance level, or that we have explicitly enabled support for it
            if (!supportedFormats.Contains(format))
            {
                return new ValidationError("The requested format is not supported. Please check the info.json and the API specification for details.", false);
            }

            return format;
        }

        public static Option<ImageQuality> ParseQuality(in string qualityString)
        {
            return Enum.TryParse(qualityString, out ImageQuality quality)
                ? quality :
                Option<ImageQuality>.None;
        }

        public static Option<ImageRegion> CalculateRegion(string region_string)
        {
            if (region_string.Length < 4)
                return Option<ImageRegion>.None;

            ImageRegionMode regionMode;
            switch (region_string.Substring(0, 4))
            {
                case "full":
                    if (region_string != "full")
                        return Option<ImageRegion>.None;
                    regionMode = ImageRegionMode.Full;
                    break;
                case "squa":
                    if (region_string != "square")
                        return Option<ImageRegion>.None;
                    regionMode = ImageRegionMode.Square;
                    break;
                case "pct:":
                    regionMode = ImageRegionMode.PercentageRegion;
                    region_string = region_string.Substring(4);
                    break;
                default:
                    regionMode = ImageRegionMode.Region;
                    // only pct: can be floating point, we expect whole pixels for normal region request
                    if (!region_string.Replace(',', '0').All(char.IsDigit))
                        return Option<ImageRegion>.None;
                    break;
            }

            switch (regionMode)
            {
                case ImageRegionMode.Region:
                case ImageRegionMode.PercentageRegion:
                    var regions = region_string.Split(Delimiter);
                    var parsed = regions.Select(r => float.TryParse(r, out var v) ? v : Option<float>.None).ToArray();
                    if (parsed.Length != 4 || parsed.Any(p => p.IsNone))
                    {
                        return Option<ImageRegion>.None;
                    }
                    return new ImageRegion(regionMode, parsed[0].IfNone(0), parsed[1].IfNone(0), parsed[2].IfNone(0), parsed[3].IfNone(0));
                default:
                    return new ImageRegion(regionMode, 0f, 0f, 0f, 0f);
            }
        }

        public static Option<ImageSize> CalculateSize(string size_string, ApiVersion apiVersion = ApiVersion.v3_0)
        {
            ImageSizeMode sizeMode;
            var percentage = 1f;
            int width = 0;
            int height = 0;

            var size_span = size_string.AsSpan();

            var upscaling = size_span[0] == '^';
            var maintain_ar = size_span[upscaling ? 1 : 0] == '!';

            var modeStart = upscaling && maintain_ar ? 2 : (upscaling || maintain_ar) ? 1 : 0;

            var sizeStart = modeStart;

            var mode = size_span.Slice(modeStart, Math.Min(size_span.Length - modeStart, 4));
            switch (mode)
            {
                // Ugh. feels like this should be a special case
                // https://github.com/dotnet/csharplang/issues/1881
                case var _ when mode.SequenceEqual("full".AsSpan()):
                    if (ApiVersion.v3_0 == apiVersion)
                        throw new ArgumentException("size full not supported in 3.0", "size");

                    sizeMode = ImageSizeMode.Max;
                    break;
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
                    else if (size_string.Split(',').Length == 2)
                    {
                        sizeMode = ImageSizeMode.Distort;
                    }
                    else
                    {
                        return Option<ImageSize>.None;
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
                    var second = sizeSpan.Slice(commaPos + 1);
                    if (second.IsEmpty && first.IsEmpty)
                    {
                        return Option<ImageSize>.None;
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