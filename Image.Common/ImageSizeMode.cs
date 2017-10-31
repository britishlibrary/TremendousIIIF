using System.ComponentModel;

namespace Image.Common
{
    public enum ImageSizeMode
    {
        [Description("The image or region is returned at the maximum size available, as indicated by maxWidth, maxHeight, maxArea in the profile description. This is the same as full if none of these properties are provided.")]
        Max = 0,
        [Description("The image content is scaled for the best fit such that the resulting width and height are less than or equal to the requested width and height. The exact scaling may be determined by the service provider, based on characteristics including image quality and system performance. The dimensions of the returned image content are calculated to maintain the aspect ratio of the extracted region.")]
        MaintainAspectRatio = 1,
        [Description("The width and height of the returned image is scaled to n% of the width and height of the extracted region. The aspect ratio of the returned image is the same as that of the extracted region.")]
        PercentageScaled = 2,
        [Description("The width and height of the returned image are exactly w and h. The aspect ratio of the returned image may be different than the extracted region, resulting in a distorted image.")]
        Distort = 3,
        [Description("The image or region is not scaled, and is returned at its full size. Note deprecation warning.")]
        Full = 4
    }
}
