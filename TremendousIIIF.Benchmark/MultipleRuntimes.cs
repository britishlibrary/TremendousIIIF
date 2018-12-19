using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace TremendousIIIF.Benchmark
{
    public class MultipleRuntimes : ManualConfig
    {
        public MultipleRuntimes()
        {
            Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp22));

            Add(Job.Default.With(CsProjClassicNetToolchain.Net471));
        }
    }
}


