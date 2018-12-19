using BenchmarkDotNet.Attributes;
using Image.Common;
using System;
using TremendousIIIF.Validation;

namespace TremendousIIIF.Benchmark.Parsing
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [Config(typeof(MultipleRuntimes))]
    [MemoryDiagnoser]
    public class RegionBenchmarks
    {
        [Params("full", "square", "pct:10,10,10,10", "pct:25.5,45.7,65.5,10.2", "pct:25.0,10,10,15.0", "256,256,256,256")]
        public string RegionString;


        [Benchmark]
        public ImageRegion Custom()
        {
            try
            {
                return ImageRequestValidator.CalculateRegion(RegionString);
            }
            catch (Exception)

            {
                return new ImageRegion();
            }
        }
    }
}
