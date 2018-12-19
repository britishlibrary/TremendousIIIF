using System.ComponentModel;

namespace Image.Common
{
    public enum ImageSizeMode
    {
        [Description("The extracted region is returned at the maximum size available. The resulting image will have the pixel dimensions of the extracted region, unless it is constrained to a smaller size by maxWidth, maxHeight, or maxArea as defined in the Technical Properties section.")]
        Max = 0,
        [Description("The image content is scaled for the best fit such that the resulting width and height are less than or equal to the requested width and height. The exact scaling may be determined by the service provider, based on characteristics including image quality and system performance. The dimensions of the returned image content are calculated to maintain the aspect ratio of the extracted region.")]
        MaintainAspectRatio = 1,
        [Description("The width and height of the returned image is scaled to n percent of the width and height of the extracted region. The value of n must not be greater than 100.")]
        PercentageScaled = 2,
        [Description("The width and height of the returned image are exactly w and h. The aspect ratio of the returned image may be different than the extracted region, resulting in a distorted image.")]
        Distort = 3,
        [Description("The image or region is not scaled, and is returned at its full size. Note deprecation warning.")]
        Full = 4
    }
}
