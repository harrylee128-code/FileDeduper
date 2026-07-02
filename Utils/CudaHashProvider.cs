using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FileDeduper.Utils
{
    internal class CudaHashProvider : IHashProvider
    {
        private const string DllName = "FileDeduperCuda.dll";

        public string Name { get { return "CUDA full-file MD5"; } }
        public bool IsHardwareAccelerated { get { return true; } }

        public static bool IsAvailable(out string reason)
        {
            reason = "";
            try
            {
                var buffer = new StringBuilder(512);
                int code = fd_cuda_is_available(buffer, buffer.Capacity);
                reason = buffer.ToString();
                return code == 0;
            }
            catch (DllNotFoundException)
            {
                reason = "CUDA provider DLL not found.";
                return false;
            }
            catch (BadImageFormatException)
            {
                reason = "CUDA provider DLL architecture is incompatible.";
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                reason = "CUDA provider DLL does not expose the expected API.";
                return false;
            }
            catch (Exception ex)
            {
                reason = "CUDA provider unavailable: " + ex.Message;
                return false;
            }
        }

        public HashComputationResult ComputeFullMd5(string path, Action<long, long> progress)
        {
            var result = new HashComputationResult();
            result.ProviderName = Name;
            result.HardwareAccelerated = true;

            var sw = Stopwatch.StartNew();
            try
            {
                var hash = new StringBuilder(33);
                var reason = new StringBuilder(512);
                ulong bytesRead;
                double nativeElapsedMs;
                int code = fd_cuda_md5_file_utf16(path, hash, hash.Capacity, reason, reason.Capacity, out bytesRead, out nativeElapsedMs);
                result.BytesRead = bytesRead > long.MaxValue ? long.MaxValue : (long)bytesRead;
                if (code == 0)
                {
                    result.Hash = hash.ToString();
                    if (progress != null) progress(result.BytesRead, result.BytesRead);
                }
                else
                {
                    result.Hash = null;
                    result.HardwareAccelerated = false;
                    result.FallbackReason = reason.ToString();
                }
            }
            catch (Exception ex)
            {
                result.Hash = null;
                result.HardwareAccelerated = false;
                result.FallbackReason = ex.Message;
            }
            finally
            {
                sw.Stop();
                result.Elapsed = sw.Elapsed;
            }
            return result;
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern int fd_cuda_is_available(StringBuilder reason, int reasonLength);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern int fd_cuda_md5_file_utf16(
            string path,
            StringBuilder hashHex,
            int hashHexLength,
            StringBuilder reason,
            int reasonLength,
            out ulong bytesRead,
            out double elapsedMs);
    }
}
