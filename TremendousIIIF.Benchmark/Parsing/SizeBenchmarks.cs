using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using Image.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TremendousIIIF.Validation;

namespace TremendousIIIF.Benchmark.Parsing
{
    public class MultipleRuntimes : ManualConfig
    {
        public MultipleRuntimes()
        {
            Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp20));

            Add(Job.Default.With(CsProjClassicNetToolchain.Net471));
        }
    }
    [HtmlExporter, CsvExporter, RPlotExporter]
    [Config(typeof(MultipleRuntimes))]
    [MemoryDiagnoser]
    public class SizeBenchmarks
    {
        [Params("full", "max", "pct:10", "pct:25.5444712736684", "256,256", "!256,256", "256,", ",256")]
        public string RegionString;

        [Benchmark]
        public ImageSize Default()
        {
            try
            {
                return ImageRequestValidator.CalculateSize(RegionString);
            }
            catch (Exception)

            {
                return new ImageSize();
            }
        }
    }
}
