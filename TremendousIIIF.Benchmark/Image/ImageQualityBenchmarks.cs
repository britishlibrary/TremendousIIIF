using System;
using SkiaSharp;
using BenchmarkDotNet.Attributes;
using Image.Common;
using BenchmarkDotNet.Attributes.Exporters;
using TremendousIIIF.Common;
using Serilog;

namespace TremendousIIIF.Benchmark.Image
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    public class ImageQualityBenchmarks
    {
        public SKImage Imagecopy { get; set; }
    
        [Params(ImageQuality.bitonal, ImageQuality.gray)]
        public ImageQuality Quality { get; set; }

        public ILogger Log { get; set; }
        [Setup]
        public void Setup()
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
                Rotation = new ImageRotation { Degrees = 0, Mirror = false },
                RequestId = string.Empty
            };
            Log = new LoggerConfiguration().CreateLogger();
            (var state, var img) = Jpeg2000.J2KExpander.ExpandRegion(null, Log, file, request, false, new Common.Configuration.ImageQuality());
            Imagecopy = img;

        }

        [Benchmark]
        public SKImage HighContrast() => ImageProcessing.ImageProcessing.AlterQualityContrastFilter(Imagecopy, Quality);

        [Benchmark(Baseline =true)]
        public SKImage Matrix() => ImageProcessing.ImageProcessing.AlterQuality(Imagecopy, Quality);
    }
}
