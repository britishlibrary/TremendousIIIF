using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using BenchmarkDotNet.Attributes;
using Jpeg2000;
using Microsoft.Extensions.Logging;
using Image.Common;
using Microsoft.Extensions.Logging.Abstractions;
using C = TremendousIIIF.Common.Configuration;
using System.Threading.Tasks;

namespace TremendousIIIF.Benchmark.JPEG2000
{
    public class ExpanderBenchmarks
    {
        public static ILogger Logger;

        public static readonly Uri FileUri = new Uri("file:///C:/Source/TremendousIIIF/TremendousIIIF.Benchmark/TestData/RoyalMS.jp2");

        [GlobalSetup]
        public void Setup()
        {
            var provider = NullLoggerProvider.Instance;
            Logger = provider.CreateLogger(string.Empty);
        }

        [Benchmark(Baseline =true)]
        public Task<(ProcessState, SKImage)> RegionCompositor() => J2KExpander.ExpandRegionCompositor(null, Logger, FileUri, Request, false, Quality);

        [Benchmark]
        public Task<(ProcessState, SKImage)> RegionDecompressor() => J2KExpander.ExpandRegionDecompressor(null, Logger, FileUri, Request, false, Quality);

        public C.ImageQuality Quality = new C.ImageQuality();
        [ParamsAllValues]
        public ImageRegionMode Mode { get; set; }

        [ParamsSource(nameof(RegionValues))]
        public ImageRegion Region { get; set; }


        [ParamsSource(nameof(SizeValues))]
        public ImageSize Size { get; set; }


        [ParamsSource(nameof(RequestValues))]
        public ImageRequest Request { get; set; }


        [ParamsSource(nameof(RotationValues))]
        public ImageRotation Rotation { get; set; }


        [Params(256, 512, 1024, 2048)]
        public int OutputWidth { get; set; }
        [Params(256, 512, 1024, 2048)]
        public int OutputHeight { get; set; }

        [Params(0.1, 0.25, 0.5, 0.75)]
        public int Percent { get; set; }

        [Params(0, 90, 45, 180)]
        public int Degrees { get; set; }

        [ParamsAllValues]
        public bool Mirror { get; set; }


        [ParamsAllValues]
        public ImageSizeMode SizeMode { get; set; }

        public IEnumerable<ImageRegion> RegionValues()
        {
            yield return new ImageRegion(Mode, 0, 0, 1024, 1024);
        }

        public IEnumerable<ImageRequest> RequestValues()
        {
            yield return new ImageRequest(Region, Size, Rotation);
        }

        public IEnumerable<ImageSize> SizeValues()
        {
            yield return new ImageSize(SizeMode, Percent, OutputWidth, OutputHeight);
        }

        public IEnumerable<ImageRotation> RotationValues()
        {
            yield return new ImageRotation(Degrees, Mirror);
        }


    }
}
