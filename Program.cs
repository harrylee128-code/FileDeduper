using System;
using System.Windows.Forms;
using FileDeduper.Forms;

namespace FileDeduper
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // 高 DPI 感知，让界面在高清屏不模糊
            try
            {
                SetProcessDpiAwarenessContext();
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        /// <summary>
        /// 尝试启用 Per-Monitor V2 DPI 感知（Win10 1703+）；
        /// 失败则回退到系统级 DPI 感知。均通过 API 直接调用，不依赖 .NET 4.7+ 的托管封装。
        /// </summary>
        private static void SetProcessDpiAwarenessContext()
        {
            // Per-Monitor V2：尝试用 SetProcessDpiAwarenessContext (-4 = PER_MONITOR_AWARE_V2)
            try
            {
                if (SetProcessDpiAwarenessContextInternal(new IntPtr(-4))) return;
            }
            catch { }
            // 回退：SetProcessDPIAware（系统级）
            try
            {
                SetProcessDPIAwareInternal();
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContextInternal(IntPtr value);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAwareInternal();
    }
}
