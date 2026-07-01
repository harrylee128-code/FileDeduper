using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FileDeduper.Utils
{
    /// <summary>
    /// 通过 SHFileOperation 将文件移入回收站（可恢复）。
    /// 一次调用可批量处理多个文件。
    /// </summary>
    internal static class RecycleBinHelper
    {
        private const int FO_DELETE = 0x0003;
        private const int FOF_ALLOWUNDO = 0x0040;   // 保留撤销信息 → 移入回收站
        private const int FOF_NOCONFIRMATION = 0x0010; // 不弹系统确认框（我们已自己确认过）
        private const int FOF_SILENT = 0x0004;      // 不显示进度 UI
        private const int FOF_NOERRORUI = 0x0400;
        private const int FOF_WANTNUKEWARNING = 0x4000; // 如将永久删除而非回收，强制交给系统警告

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        /// <summary>
        /// 将多个文件移入回收站。
        /// 返回无法回收的文件路径列表（空表示全部成功）。
        /// </summary>
        public static List<string> SendToRecycleBin(IList<string> paths)
        {
            var failed = new List<string>();
            if (paths == null || paths.Count == 0) return failed;

            var safePaths = new List<string>();
            foreach (string p in paths)
            {
                if (IsKnownRecycleBinSupportedPath(p))
                {
                    safePaths.Add(p);
                }
                else
                {
                    failed.Add(p);
                }
            }
            if (safePaths.Count == 0) return failed;

            // SHFileOperation 要求多个路径以 \0 分隔，结尾双 \0。
            var sb = new StringBuilder();
            foreach (string p in safePaths)
            {
                sb.Append(p).Append('\0');
            }
            sb.Append('\0');

            var op = new SHFILEOPSTRUCT();
            op.hwnd = IntPtr.Zero;
            op.wFunc = FO_DELETE;
            op.pFrom = sb.ToString();
            op.pTo = null;
            op.fFlags = RecycleFlags;
            op.fAnyOperationsAborted = false;
            op.hNameMappings = IntPtr.Zero;
            op.lpszProgressTitle = null;

            try
            {
                int rc = SHFileOperation(ref op);
                if (rc != 0 || op.fAnyOperationsAborted)
                {
                    // SHFileOperation 失败时不告诉具体哪个文件失败，统一回退逐个处理以定位失败项。
                    failed.AddRange(RecycleOneByOne(safePaths));
                }
            }
            catch
            {
                failed.AddRange(RecycleOneByOne(safePaths));
            }
            return failed;
        }

        public static bool IsKnownRecycleBinSupportedPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return false;
                if (path.StartsWith(@"\\", StringComparison.Ordinal)) return false;

                string root = Path.GetPathRoot(path);
                if (string.IsNullOrWhiteSpace(root)) return false;

                var drive = new DriveInfo(root);
                return drive.IsReady
                    && drive.DriveType == DriveType.Fixed
                    && IsInteractiveUserSession()
                    && !IsKnownAutomationSession()
                    && CanAccessRecycleBinShell();
            }
            catch
            {
                return false;
            }
        }

        public static bool IsInteractiveUserSession()
        {
            try
            {
                return Environment.UserInteractive
                    && Process.GetCurrentProcess().SessionId != 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsKnownAutomationSession()
        {
            return IsTruthy(Environment.GetEnvironmentVariable("CI"))
                || IsTruthy(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
                || IsTruthy(Environment.GetEnvironmentVariable("TF_BUILD"))
                || IsTruthy(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"))
                || IsTruthy(Environment.GetEnvironmentVariable("JENKINS_URL"));
        }

        private static bool IsTruthy(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanAccessRecycleBinShell()
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return false;

                object shell = Activator.CreateInstance(shellType);
                object recycleBin = shellType.InvokeMember("NameSpace",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { 10 });
                if (recycleBin == null) return false;

                object items = recycleBin.GetType().InvokeMember("Items",
                    System.Reflection.BindingFlags.InvokeMethod, null, recycleBin, null);
                return items != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>逐个回收，便于定位失败的具体文件。</summary>
        private static List<string> RecycleOneByOne(IList<string> paths)
        {
            var failed = new List<string>();
            foreach (string p in paths)
            {
                var sb = new StringBuilder();
                sb.Append(p).Append('\0').Append('\0');
                var op = new SHFILEOPSTRUCT();
                op.hwnd = IntPtr.Zero;
                op.wFunc = FO_DELETE;
                op.pFrom = sb.ToString();
                op.pTo = null;
                op.fFlags = RecycleFlags;
                op.fAnyOperationsAborted = false;
                op.hNameMappings = IntPtr.Zero;
                op.lpszProgressTitle = null;

                try
                {
                    // 检查文件是否存在
                    if (!System.IO.File.Exists(p))
                    {
                        failed.Add(p);
                        continue;
                    }

                    int rc = SHFileOperation(ref op);
                    if (rc != 0 || op.fAnyOperationsAborted) failed.Add(p);
                }
                catch
                {
                    failed.Add(p);
                }
            }
            return failed;
        }

        internal static ushort RecycleFlags
        {
            get
            {
                return FOF_ALLOWUNDO
                    | FOF_NOCONFIRMATION
                    | FOF_SILENT
                    | FOF_NOERRORUI
                    | FOF_WANTNUKEWARNING;
            }
        }
    }
}
