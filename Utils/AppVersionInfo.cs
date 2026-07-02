using System;
using System.IO;

namespace FileDeduper.Utils
{
    internal static class AppVersionInfo
    {
        public const string DisplayVersion = "v2.2.0-preview.1";
        public const string LitePackageVersion = "2.2.0-preview1-lite";
        public const string CudaPackageVersion = "2.2.0-preview1-cuda";

        public static string PackageChannel
        {
            get
            {
                string reason;
                if (CudaHashProvider.IsAvailable(out reason)) return "CUDA";
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileDeduperCuda.dll"))) return "CUDA fallback";
                return "Lite";
            }
        }

        public static string WindowTitle
        {
            get { return "FileDeduper " + DisplayVersion + " " + PackageChannel + " · 文件查重清理助手"; }
        }

        public static string AboutVersionLine
        {
            get { return "版本：" + DisplayVersion + " (" + PackageChannel + ")"; }
        }
    }
}
