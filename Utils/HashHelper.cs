using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileDeduper.Utils
{
    /// <summary>
    /// MD5 哈希计算工具。
    /// 始终全量分块读取，确保“已验证”结果是真实完整文件哈希一致。
    /// 流式读取不落地临时文件，绿色无污染。
    /// </summary>
    internal static class HashHelper
    {
        private const int BufferSize = 1 * 1024 * 1024; // 1 MB

        /// <summary>
        /// 计算完整文件 MD5。用于“已验证”重复判断。
        /// </summary>
        public static string ComputeFullMd5(string path, Action<long, long> progress)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    return ComputeFull(fs, fs.Length, progress);
                }
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeFull(FileStream fs, long total, Action<long, long> progress)
        {
            using (var md5 = MD5.Create())
            {
                byte[] buffer = new byte[BufferSize];
                long done = 0;
                int read;
                while ((read = fs.Read(buffer, 0, BufferSize)) > 0)
                {
                    md5.TransformBlock(buffer, 0, read, buffer, 0);
                    done += read;
                    if (progress != null) progress(done, total);
                }
                md5.TransformFinalBlock(buffer, 0, 0);
                return BytesToHex(md5.Hash);
            }
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
