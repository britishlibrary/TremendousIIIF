using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using Image.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TremendousIIIF.Validation;

namespace TremendousIIIF.Benchmark.Parsing
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [MemoryDiagnoser]
    public class SizeBenchmarks
    {
        [Params("full", "max", "pct:10", "pct:25.5444712736684", "256,256", "!256,256", "256,", ",256")]
        public string RegionString;

        [Benchmark]
        public ImageSize Custom()
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
