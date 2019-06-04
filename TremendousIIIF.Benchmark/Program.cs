using BenchmarkDotNet.Running;
using TremendousIIIF.Benchmark.Image;
using TremendousIIIF.Benchmark.Parsing;
using TremendousIIIF.Benchmark.TIFF;
using BenchmarkDotNet.Attributes;
using TremendousIIIF.Benchmark.JPEG2000;

namespace TremendousIIIF.Benchmark
{
    [SimpleJob(launchCount: 1, warmupCount: 1, targetCount: 1, invocationCount: 1, id: "QuickJob")]
    [ShortRunJob]
    class Program
    {

        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[] {
                typeof(ImageEncodingBenchmarks),
                typeof(RegionBenchmarks),
                typeof(SizeBenchmarks),
                typeof(PipelineBenchmarks),
                typeof(TiffExpanderBenchmarks),
                typeof(ImageDPIBenchmarks),
                typeof(ExpanderBenchmarks)
            });
            switcher.Run(args);
        }


    }
}
