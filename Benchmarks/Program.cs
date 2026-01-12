using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<HTD_Analyzer.Benchmarks.AnalyzerBenchmarks>();
        }
    }
}
