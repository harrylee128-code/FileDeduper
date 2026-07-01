using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public int RequestedParallelism;
        public int EffectiveParallelism;

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
            return Run(paths, mode, 1);
        }

        public static HashBenchmarkResult Run(IEnumerable<string> paths, HardwareAccelerationMode mode, int hashParallelism)
        {
            var result = new HashBenchmarkResult();
            result.RequestedParallelism = HashParallelism.NormalizeForSettings(hashParallelism);
            result.EffectiveParallelism = HashParallelism.Resolve(hashParallelism);
            var validPaths = paths == null
                ? new string[0]
                : paths.Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path)).ToArray();
            var sw = Stopwatch.StartNew();

            if (result.EffectiveParallelism <= 1 || validPaths.Length <= 1)
            {
                foreach (string path in validPaths)
                {
                    AddHashResult(result, HashEngine.ComputeFullMd5(path, mode, null));
                }
            }
            else
            {
                var options = new ParallelOptions();
                options.MaxDegreeOfParallelism = result.EffectiveParallelism;
                Parallel.ForEach(validPaths, options, path =>
                {
                    AddHashResult(result, HashEngine.ComputeFullMd5(path, mode, null));
                });
            }

            sw.Stop();
            result.Elapsed = sw.Elapsed;
            return result;
        }

        private static void AddHashResult(HashBenchmarkResult result, HashComputationResult hash)
        {
            Interlocked.Increment(ref result.FileCount);
            Interlocked.Add(ref result.TotalBytes, hash.BytesRead);
            lock (result)
            {
                result.Provider = hash.ProviderName;
                if (!string.IsNullOrEmpty(hash.FallbackReason)) result.FallbackReason = hash.FallbackReason;
            }
        }
    }
}
