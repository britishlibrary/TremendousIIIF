using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using TremendousIIIF.Benchmark.Image;
using TremendousIIIF.Benchmark.JPEG2000;
using TremendousIIIF.Benchmark.Parsing;
using TremendousIIIF.Benchmark.TIFF;
using Xunit.Abstractions;

namespace TremendousIIIF.Benchmark
{
    public class Benchmarks
    {
        private readonly ITestOutputHelper output;
        private readonly AccumulationLogger logger;
        private readonly ManualConfig config;

        public Benchmarks(ITestOutputHelper output)
        {
            this.output = output;
            logger = new AccumulationLogger();

            config = ManualConfig.Create(DefaultConfig.Instance)
                .AddLogger(logger)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }

        [Fact]
        public void Run_Region_Benchmarks()
        {
            BenchmarkRunner.Run<RegionBenchmarks>(config);
            // write benchmark summary
            output.WriteLine(logger.GetLog());
        }

        [Fact]
        public void Run_Size_Benchmarks()
        {
            BenchmarkRunner.Run<SizeBenchmarks>(config);
            // write benchmark summary
            output.WriteLine(logger.GetLog());
        }

        [Fact]
        public void Run_Expander_Benchmarks()
        {
            BenchmarkRunner.Run<ExpanderBenchmark>(config);
            // write benchmark summary
            output.WriteLine(logger.GetLog());
        }

        [Fact]
        public void Run_ImageEncoding_Benchmarks()
        {
            BenchmarkRunner.Run<ImageEncodingBenchmarks>(config);
            // write benchmark summary
            output.WriteLine(logger.GetLog());
        }

        [Fact]
        public void Run_ImageDpi_Benchmarks()
        {
            BenchmarkRunner.Run<ImageDPIBenchmarks>(config);
            // write benchmark summary
            output.WriteLine(logger.GetLog());
        }

        [Fact]
        public void Run_TiffExpander_Benchmarks()
        {
            BenchmarkRunner.Run<TiffExpanderBenchmarks>(config);
            // write benchmark summary
            output.WriteLine(logger.GetLog());
        }
    }
}
