using System;
using FileDeduper.Models;

namespace FileDeduper.Utils
{
    /// <summary>
    /// MD5 哈希计算工具。
    /// 始终全量分块读取，确保“已验证”结果是真实完整文件哈希一致。
    /// 流式读取不落地临时文件，绿色无污染。
    /// </summary>
    internal static class HashHelper
    {
        /// <summary>
        /// 计算完整文件 MD5。用于“已验证”重复判断。
        /// </summary>
        public static string ComputeFullMd5(string path, Action<long, long> progress)
        {
            return ComputeFullMd5(path, HardwareAccelerationMode.Auto, progress);
        }

        public static string ComputeFullMd5(string path, HardwareAccelerationMode mode, Action<long, long> progress)
        {
            return HashEngine.ComputeFullMd5(path, mode, progress).Hash;
        }
    }
}
