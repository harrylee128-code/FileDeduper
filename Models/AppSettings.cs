using System.Collections.Generic;
using FileDeduper.Models;

namespace FileDeduper.Models
{
    /// <summary>
    /// 应用配置，序列化为 exe 同目录 JSON 文件，实现绿色便携。
    /// </summary>
    public class AppSettings
    {
        /// <summary>上次选择的扫描文件夹列表（便于下次复用）。</summary>
        public List<string> LastFolders { get; set; }

        /// <summary>是否包含子目录。</summary>
        public bool IncludeSubdirectories { get; set; }

        /// <summary>默认删除方式。</summary>
        public DeleteMode DeleteMode { get; set; }

        /// <summary>默认保留策略。</summary>
        public KeepStrategy KeepStrategy { get; set; }

        /// <summary>是否对完全重复组也做哈希复核。</summary>
        public bool HashVerifyLikelyGroups { get; set; }

        /// <summary>哈希计算加速模式。</summary>
        public HardwareAccelerationMode HardwareAccelerationMode { get; set; }

        /// <summary>最小文件大小(字节)，小于此值的文件忽略。</summary>
        public long MinFileSize { get; set; }

        public AppSettings()
        {
            LastFolders = new List<string>();
            IncludeSubdirectories = true;
            DeleteMode = DeleteMode.Recycle;
            KeepStrategy = KeepStrategy.Oldest;
            HashVerifyLikelyGroups = false;
            HardwareAccelerationMode = HardwareAccelerationMode.Auto;
            MinFileSize = 0;
        }
    }
}
