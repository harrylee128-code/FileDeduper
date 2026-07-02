using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FileDeduper.Models;

namespace FileDeduper.Utils
{
    internal class HashComputationResult
    {
        public string Hash;
        public string ProviderName;
        public bool HardwareAccelerated;
        public string FallbackReason;
        public long BytesRead;
        public TimeSpan Elapsed;

        public HashComputationResult()
        {
            Hash = null;
            ProviderName = "CPU";
            FallbackReason = "";
        }
    }

    internal interface IHashProvider
    {
        string Name { get; }
        bool IsHardwareAccelerated { get; }
        HashComputationResult ComputeFullMd5(string path, Action<long, long> progress);
    }

    internal static class HashEngine
    {
        public static HashComputationResult ComputeFullMd5(
            string path,
            HardwareAccelerationMode mode,
            Action<long, long> progress)
        {
            string selectionReason;
            IHashProvider provider = SelectProvider(mode, out selectionReason);
            var result = provider.ComputeFullMd5(path, progress);

            if (provider.IsHardwareAccelerated && string.IsNullOrEmpty(result.Hash))
            {
                string cudaReason = string.IsNullOrEmpty(result.FallbackReason) ? selectionReason : result.FallbackReason;
                var fallback = new CpuHashProvider().ComputeFullMd5(path, progress);
                fallback.FallbackReason = "CUDA provider failed; CPU fallback used. " + cudaReason;
                return fallback;
            }

            if (mode == HardwareAccelerationMode.GpuExperimental && !result.HardwareAccelerated)
            {
                var caps = HardwareCapabilityDetector.Detect();
                result.FallbackReason = !string.IsNullOrEmpty(selectionReason)
                    ? selectionReason
                    : string.IsNullOrEmpty(caps.Reason)
                    ? "GPU provider unavailable; CPU fallback used."
                    : caps.Reason;
            }
            else if (mode == HardwareAccelerationMode.Auto && !result.HardwareAccelerated)
            {
                result.FallbackReason = "Auto mode selected CPU provider; no safe optional GPU provider is installed.";
            }

            return result;
        }

        public static string Describe(HardwareAccelerationMode mode)
        {
            var caps = HardwareCapabilityDetector.Detect();
            if (mode == HardwareAccelerationMode.CpuOnly)
            {
                return "CPU only";
            }
            if (mode == HardwareAccelerationMode.GpuExperimental)
            {
                string cudaReason;
                return CudaHashProvider.IsAvailable(out cudaReason)
                    ? "CUDA provider available: " + cudaReason
                    : "CUDA provider unavailable: " + cudaReason;
            }
            return caps.NvidiaSmiAvailable
                ? "Auto: " + caps.Reason
                : "Auto: CPU provider";
        }

        private static IHashProvider SelectProvider(HardwareAccelerationMode mode, out string reason)
        {
            reason = "";
            if (mode == HardwareAccelerationMode.GpuExperimental)
            {
                if (CudaHashProvider.IsAvailable(out reason))
                {
                    return new CudaHashProvider();
                }
                return new CpuHashProvider();
            }

            // Native CUDA/Intel providers are intentionally optional. Until a redistributable
            // provider is present, CPU remains the correctness baseline.
            return new CpuHashProvider();
        }
    }

    internal class CpuHashProvider : IHashProvider
    {
        private const int BufferSize = 4 * 1024 * 1024;

        public string Name { get { return "CPU full-file MD5"; } }
        public bool IsHardwareAccelerated { get { return false; } }

        public HashComputationResult ComputeFullMd5(string path, Action<long, long> progress)
        {
            var result = new HashComputationResult();
            result.ProviderName = Name;
            result.HardwareAccelerated = false;

            var sw = Stopwatch.StartNew();
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                using (var md5 = MD5.Create())
                {
                    byte[] buffer = new byte[BufferSize];
                    long total = fs.Length;
                    long done = 0;
                    int read;
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        md5.TransformBlock(buffer, 0, read, buffer, 0);
                        done += read;
                        result.BytesRead = done;
                        if (progress != null) progress(done, total);
                    }
                    md5.TransformFinalBlock(buffer, 0, 0);
                    result.Hash = BytesToHex(md5.Hash);
                }
            }
            catch
            {
                result.Hash = null;
            }
            finally
            {
                sw.Stop();
                result.Elapsed = sw.Elapsed;
            }
            return result;
        }

        private static string BytesToHex(byte[] bytes)
        {
            if (bytes == null) return null;
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
