using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HTD_Analyzer
{
    // Minimal analysis result model
    public class AnalysisFinding
    {
        public string Location { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string FontName { get; set; } = string.Empty;
        public double? FontSize { get; set; }
        public List<string> HiddenReasons { get; set; } = new List<string>();
    }

    public class HTDAnalysisResults
    {
        // Synchronous wrapper kept for compatibility
        public List<AnalysisFinding> AnalyzeFile(string path)
        {
            return AnalyzeFileAsync(path, CancellationToken.None).GetAwaiter().GetResult();
        }

        // Async analyzer using a small buffer; ArrayPool removed for compatibility
        public async Task<List<AnalysisFinding>> AnalyzeFileAsync(string path, CancellationToken cancellationToken = default)
        {
            var findings = new List<AnalysisFinding>();

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return findings;

            var fileInfo = new FileInfo(path);

            // Use a small buffer allocated on the heap to avoid external package dependency
            const int BufferSize = 1024;
            var buffer = new byte[BufferSize];
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                {
                    int read = await fs.ReadAsync(buffer, 0, BufferSize, cancellationToken).ConfigureAwait(false);
                    if (read > 0)
                    {
                        // Create a small hex snippet (first N bytes) using a char array
                        const int SnippetBytes = 32;
                        int take = Math.Min(SnippetBytes, read);
                        int charsNeeded = take * 2; // two hex chars per byte
                        var charBuf = new char[charsNeeded];

                        int pos = 0;
                        for (int i = 0; i < take; i++)
                        {
                            byte b = buffer[i];
                            int hi = (b >> 4) & 0xF;
                            int lo = b & 0xF;
                            charBuf[pos++] = (char)(hi < 10 ? ('0' + hi) : ('A' + (hi - 10)));
                            charBuf[pos++] = (char)(lo < 10 ? ('0' + lo) : ('A' + (lo - 10)));
                        }

                        var snippet = new string(charBuf, 0, pos);

                        findings.Add(new AnalysisFinding
                        {
                            Location = fileInfo.Name,
                            Text = snippet,
                            FontName = "Unknown",
                            FontSize = null,
                            HiddenReasons = new List<string>()
                        });
                    }
                }
            }
            finally
            {
                // nothing to return to pool
            }

            return findings;
        }
    }
}
