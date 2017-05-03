using System;
using SkiaSharp;
using BenchmarkDotNet.Attributes;
using Image.Common;
using BenchmarkDotNet.Attributes.Exporters;
using TremendousIIIF.Common;

namespace TremendousIIIF.Benchmark.Image
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [MemoryDiagnoser]
    public class ImageQualityBenchmarks
    {
        SKBitmap _bmp;
        private SKImage Imagecopy
        {
            get
            {
                return SKImage.FromBitmap(_bmp);
            }
        }
        [Params(ImageQuality.bitonal, ImageQuality.gray)]
        public ImageQuality Quality { get; set; }
        public ImageQualityBenchmarks()
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
            _bmp = SKBitmap.FromImage(img);
        }

        [Benchmark]
        public SKImage AlterColor() => ImageProcessing.ImageProcessing.AlterQuality(Imagecopy, Quality);

        [Benchmark(Baseline =true)]
        public SKImage Color() => ImageProcessing.ImageProcessing.AlterQuality(Imagecopy, ImageQuality.color);
    }
}
