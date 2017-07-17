using BenchmarkDotNet.Attributes;
using Image.Common;
using SkiaSharp;
using System;
using System.IO;
using TremendousIIIF.Common;
using Serilog;

namespace TremendousIIIF.Benchmark.Image
{
    public class ImageEncodingBenchmarks
    {
        public static SKImage Image { get; set; }
        [Params(SKEncodedImageFormat.Jpeg)]
        public SKEncodedImageFormat Format { get; set; }
        [GlobalSetup]
        public static void SetUp()
        {
            if (Image == null)
            {
                var file = new Uri("file:///C:/Source/TremendousIIIF/TremendousIIIF.Benchmark/TestData/RoyalMS.jp2");
                var request = new ImageRequest
                {
                    Region = new ImageRegion
                    {
                        X = 0,
                        Y = 0,
                        Mode = ImageRegionMode.Full
                    },
                    Size = new ImageSize
                    {
                        Mode = ImageSizeMode.Max,
                        Percent = 1
                    },
                    Format = ImageFormat.jpg,
                    Quality = ImageQuality.bitonal,
                    Rotation = new ImageRotation { Degrees = 0, Mirror = false }
                };
                (var state, var img) = Jpeg2000.J2KExpander.ExpandRegion(null, null, file, request, false, new Common.Configuration.ImageQuality());
                Image = img;
            }
        }

        [Benchmark]
        public Stream Encode() => ImageProcessing.ImageProcessing.EncodeSkiaImage(Image, Format, 80);
    }
}
