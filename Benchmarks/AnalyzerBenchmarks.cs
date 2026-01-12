using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HTD_Analyzer.Benchmarks
{
    [MemoryDiagnoser]
    public class AnalyzerBenchmarks
    {
        private HTD_Analyzer.HTDAnalysisResults _analyzer;
        private string _sampleFile;

        [GlobalSetup]
        public void Setup()
        {
            _analyzer = new HTD_Analyzer.HTDAnalysisResults();
            // Prefer using files copied into Benchmarks/input
            var inputDir = Path.Combine(Environment.CurrentDirectory, "input");
            string candidate = null;

            if (Directory.Exists(inputDir))
            {
                var files = Directory.GetFiles(inputDir);
                if (files.Length > 0)
                {
                    candidate = files[0];
                }
            }

            if (!string.IsNullOrEmpty(candidate))
            {
                _sampleFile = candidate;
            }
            else
            {
                _sampleFile = Path.Combine(Environment.CurrentDirectory, "sample.pdf");
                // create a small sample file if not exists
                if (!File.Exists(_sampleFile))
                    File.WriteAllText(_sampleFile, new string('A', 1024 * 10));
            }
        }

        [Benchmark]
        public async Task<List<HTD_Analyzer.AnalysisFinding>> AnalyzeSampleAsync()
        {
            return await _analyzer.AnalyzeFileAsync(_sampleFile);
        }
    }
}
