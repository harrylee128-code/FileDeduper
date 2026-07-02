namespace FileDeduper.Utils
{
    internal static class AppVersionInfo
    {
        public const string DisplayVersion = "v2.1.0-preview.3";
        public const string PackageChannel = "Lite";
        public const string PackageVersion = "2.1.0-preview3-lite";

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
