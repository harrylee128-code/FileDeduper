using System;
using System.Diagnostics;
using System.Text;

namespace FileDeduper.Utils
{
    internal class HardwareCapabilities
    {
        public bool NvidiaSmiAvailable;
        public string NvidiaSummary;
        public bool CudaToolkitAvailable;
        public string Reason;

        public HardwareCapabilities()
        {
            NvidiaSummary = "";
            Reason = "";
        }
    }

    internal static class HardwareCapabilityDetector
    {
        private static HardwareCapabilities _cached;

        public static HardwareCapabilities Detect()
        {
            if (_cached != null) return _cached;

            var result = new HardwareCapabilities();
            result.NvidiaSummary = RunTool("nvidia-smi", "--query-gpu=name,driver_version,memory.total --format=csv,noheader");
            result.NvidiaSmiAvailable = !string.IsNullOrWhiteSpace(result.NvidiaSummary);

            string nvcc = RunTool("nvcc", "--version");
            result.CudaToolkitAvailable = !string.IsNullOrWhiteSpace(nvcc);

            if (result.NvidiaSmiAvailable && !result.CudaToolkitAvailable)
            {
                result.Reason = "检测到 NVIDIA GPU/driver，但未检测到 CUDA Toolkit/nvcc；CUDA provider 只能作为后续可选 native 模块。";
            }
            else if (!result.NvidiaSmiAvailable)
            {
                result.Reason = "未检测到 nvidia-smi；当前机器没有可直接识别的 NVIDIA CUDA 环境。";
            }
            else
            {
                result.Reason = "检测到 NVIDIA GPU 与 CUDA Toolkit。";
            }

            _cached = result;
            return result;
        }

        private static string RunTool(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = fileName;
                psi.Arguments = arguments;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (var p = Process.Start(psi))
                {
                    if (p == null) return "";
                    var sb = new StringBuilder();
                    if (!p.WaitForExit(3000))
                    {
                        try { p.Kill(); } catch { }
                        return "";
                    }
                    sb.Append(p.StandardOutput.ReadToEnd());
                    if (p.ExitCode != 0 && sb.Length == 0) sb.Append(p.StandardError.ReadToEnd());
                    return sb.ToString().Trim();
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
