using BenchmarkDotNet.Running;

namespace BetterAPI.Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<StringBuilderPoolBenchmarks>();
        }
    }
}
