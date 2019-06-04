using BenchmarkDotNet.Attributes;
using Image.Common;
using SkiaSharp;
using System;
using System.IO;
using TremendousIIIF.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TremendousIIIF.Benchmark.Image
{
    public class ImageEncodingBenchmarks
    {
        public static SKSurface Image { get; set; }
        [Params(SKEncodedImageFormat.Jpeg)]
        public SKEncodedImageFormat Format { get; set; }

        public static ILogger Log = new LoggerFactory().CreateLogger("test");
        [GlobalSetup]
        public static async Task SetUp()
        {
            if (Image == null)
            {
                var file = new Uri("file:///C:/Source/TremendousIIIF/TremendousIIIF.Benchmark/TestData/RoyalMS.jp2");
                var request = new ImageRequest(new ImageRegion(ImageRegionMode.Full), new ImageSize(ImageSizeMode.Max, 1), new ImageRotation(0, false), ImageQuality.bitonal, ImageFormat.jpg);
                (var state, var img) = Jpeg2000.J2KExpander.ExpandRegion(null, Log, file, request, false, new Common.Configuration.ImageQuality());
                Image = SKSurface.Create(new SKImageInfo(img.Width, img.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
                Image.Canvas.DrawImage(img, 0, 0);
            }
        }

        [Benchmark]
        public Stream Encode() => ImageProcessing.ImageProcessing.EncodeSkiaImage(Image, Format, 80, 300, 300);
    }
}
