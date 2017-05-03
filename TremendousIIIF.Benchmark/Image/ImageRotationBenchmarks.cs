using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using Image.Common;
using SkiaSharp;
using System;
using TremendousIIIF.Common;

namespace TremendousIIIF.Benchmark.Image
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [MemoryDiagnoser]
    public class ImageRotationBenchmarks
    {
        SKBitmap _bmp;
        private SKImage Imagecopy
        {
            get
            {
                return SKImage.FromBitmap(_bmp);
            }
        }

        [Params(90,180,270,45)]
        public float Angle { get; set; }

        public ImageRotationBenchmarks()
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

        [Benchmark(Baseline = true)]
        public SKImage RotationIdentity() => ImageProcessing.ImageProcessing.RotateImage(Imagecopy, new ImageRotation { Degrees = 0 });

        [Benchmark]
        public SKImage Rotation() => ImageProcessing.ImageProcessing.RotateImage(Imagecopy, new ImageRotation { Degrees = Angle });
        
    }
}
