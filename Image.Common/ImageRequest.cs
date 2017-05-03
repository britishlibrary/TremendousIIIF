using TremendousIIIF.Common;
namespace Image.Common
{
    public class ImageRequest
    {
        public string ID { get; set; }
        public ImageRegion Region { get; set; }
        public ImageSize Size { get; set; }
        public ImageRotation Rotation { get; set; }
        public ImageQuality Quality { get; set; }
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
