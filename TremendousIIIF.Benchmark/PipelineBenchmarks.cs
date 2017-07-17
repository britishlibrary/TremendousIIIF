using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using Image.Common;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using TremendousIIIF.Common;

namespace TremendousIIIF.Benchmark
{

    public class PipelineBenchmarks
    {
        public ImageProcessing.ImageProcessing IP { get; set; }

        public Uri ImageUri { get; set; }

        public ImageRequest Request { get; set; }

        public Common.Configuration.ImageQuality Quality { get; set; }

        public bool AllowSizeAboveFull { get; set; }
        [GlobalSetup]
        public void Setup()
        {
            IP = new ImageProcessing.ImageProcessing { Log = new LoggerConfiguration().CreateLogger() };
            ImageUri = new Uri("file:///C:/Source/TremendousIIIF/TremendousIIIF.Benchmark/TestData/RoyalMS.jp2");
            Quality = new Common.Configuration.ImageQuality();
            Request = new ImageRequest
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
                Quality = ImageQuality.gray,
                Rotation = new ImageRotation { Degrees = 0, Mirror = false }
            };
        }

        [Benchmark]
        public Task<Stream> ProcessImageOld()
        {
            return IP.ProcessImage(ImageUri, Request, Quality, AllowSizeAboveFull, null);
        }

        [Benchmark]
        public Task<Stream> ProcessImage()
        {
            return IP.ProcessImage(ImageUri, Request, Quality, AllowSizeAboveFull, null);
        }
    }
}