namespace TremendousIIIF.Common
{
    public enum ImageFormat
    {
        [ImageFormatMetadata("image/jpeg")]
        jpg = 0,
        [ImageFormatMetadata("image/tiff")]
        tif = 1,
        [ImageFormatMetadata("image/png")]
        png = 2,
        [ImageFormatMetadata("image/gif")]
        gif = 3,
        [ImageFormatMetadata("image/jp2")]
        jp2 = 4,
        [ImageFormatMetadata("application/pdf")]
        pdf = 5,
        [ImageFormatMetadata("image/webp")]
        webp = 6
    }
}
