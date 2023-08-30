using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Image.Common;
using LanguageExt;
using TremendousIIIF.Validation;

namespace TremendousIIIF.Benchmark.Parsing
{
    [HtmlExporter, CsvExporter, RPlotExporter]
    [MemoryDiagnoser]
    public class RegionBenchmarks
    {
        [Params("full", "square", "pct:10,10,10,10", "pct:25.5,45.7,65.5,10.2", "pct:25.0,10,10,15.0", "256,256,256,256")]
        public string RegionString;


        [Benchmark]
        public Option<ImageRegion> Custom()
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
