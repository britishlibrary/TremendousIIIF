using TremendousIIIF.Common;
namespace Image.Common
{
    public class ImageRequest
    {
        public string ID { get; set; }
        /// <summary>
        /// The region parameter defines the rectangular portion of the full image to be returned.
        /// </summary>
        public ImageRegion Region { get; set; }
        /// <summary>
        /// The size parameter determines the dimensions to which the extracted region is to be scaled.
        /// </summary>
        public ImageSize Size { get; set; }
        /// <summary>
        /// The rotation parameter specifies mirroring and rotation
        /// </summary>
        public ImageRotation Rotation { get; set; }
        /// <summary>
        ///  The quality parameter determines whether the image is delivered in color, grayscale or black and white.
        /// </summary>
        public ImageQuality Quality { get; set; }
        /// <summary>
        /// The format of the returned image is expressed as an extension at the end of the URI.
        /// </summary>
        public ImageFormat Format { get; set; }
        public string RequestId { get; set; }
        public int MaxWidth { get; set; }
        public int MaxHeight { get; set; }
        public int MaxArea { get; set; }
        public ImageRequest()
        {
            MaxArea = int.MaxValue;
            MaxWidth = int.MaxValue;
            MaxHeight = int.MaxValue;
        }
    }
}
