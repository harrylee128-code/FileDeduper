using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FileDeduper.Models;

namespace FileDeduper.Utils
{
    internal class HashBenchmarkResult
    {
        public int FileCount;
        public long TotalBytes;
        public TimeSpan Elapsed;
        public string Provider;
        public string FallbackReason;

        public double MegabytesPerSecond
        {
            get
            {
                if (Elapsed.TotalSeconds <= 0) return 0;
                return (TotalBytes / 1024.0 / 1024.0) / Elapsed.TotalSeconds;
            }
        }

        public HashBenchmarkResult()
        {
            Provider = "";
            FallbackReason = "";
        }
    }

    internal static class HashBenchmark
    {
        public static HashBenchmarkResult Run(IEnumerable<string> paths, HardwareAccelerationMode mode)
        {
            var result = new HashBenchmarkResult();
            var sw = Stopwatch.StartNew();

            foreach (string path in paths)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                var hash = HashEngine.ComputeFullMd5(path, mode, null);
                result.FileCount++;
                result.TotalBytes += hash.BytesRead;
                result.Provider = hash.ProviderName;
                if (!string.IsNullOrEmpty(hash.FallbackReason)) result.FallbackReason = hash.FallbackReason;
            }

            sw.Stop();
            result.Elapsed = sw.Elapsed;
            return result;
        }
    }
}
