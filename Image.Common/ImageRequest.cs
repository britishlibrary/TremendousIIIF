using TremendousIIIF.Common;
namespace Image.Common
{
    public readonly struct ImageRequest
    {
        /// <summary>
        /// The region parameter defines the rectangular portion of the full image to be returned.
        /// </summary>
        public ImageRegion Region { get; }
        /// <summary>
        /// The size parameter determines the dimensions to which the extracted region is to be scaled.
        /// </summary>
        public ImageSize Size { get; }
        /// <summary>
        /// The rotation parameter specifies mirroring and rotation
        /// </summary>
        public ImageRotation Rotation { get; }
        /// <summary>
        ///  The quality parameter determines whether the image is delivered in color, grayscale or black and white.
        /// </summary>
        public ImageQuality Quality { get; }
        /// <summary>
        /// The format of the returned image is expressed as an extension at the end of the URI.
        /// </summary>
        public ImageFormat Format { get; }
        /// <summary>
        /// The unique identifier for this request
        /// </summary>
        public string RequestId { get; }
        /// <summary>
        /// The maximum width, in pixels, for this request
        /// </summary>
        public int MaxWidth { get; }
        /// <summary>
        /// The maximum height, in pixels, for this request
        /// </summary>
        public int MaxHeight { get; }
        /// <summary>
        /// The maximum area, in pixels, for this request
        /// </summary>
        public int MaxArea { get; }
        public ImageRequest(string requestId, ImageRegion region, ImageSize size, ImageRotation rotation, ImageQuality quality, ImageFormat format, int maxWidth, int maxHeight, int maxArea)
        {
            Region = region;
            Size = size;
            Rotation = rotation;
            Quality = quality;
            Format = format;
            RequestId = requestId;
            MaxArea = maxArea;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
        }

        public ImageRequest(string requestId, ImageRegion region, ImageSize size, ImageRotation rotation)
        {
            Region = region;
            Size = size;
            Rotation = rotation;
            Quality = ImageQuality.@default;
            Format = ImageFormat.jpg;
            RequestId = requestId;
            MaxArea = int.MaxValue;
            MaxWidth = int.MaxValue;
            MaxHeight = int.MaxValue;
        }
        public ImageRequest(string requestId, ImageRegion region, ImageSize size, ImageRotation rotation, ImageQuality quality, ImageFormat format)
        {
            Region = region;
            Size = size;
            Rotation = rotation;
            Quality = quality;
            Format = format;
            RequestId = requestId;
            MaxArea = int.MaxValue;
            MaxWidth = int.MaxValue;
            MaxHeight = int.MaxValue;
        }
    }
}
