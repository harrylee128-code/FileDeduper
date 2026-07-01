using System;
using System.IO;

namespace FileDeduper.Models
{
    /// <summary>
    /// 单个文件的元数据条目，贯穿扫描、查重、标记、删除全流程。
    /// </summary>
    public class FileEntry
    {
        /// <summary>文件全路径。</summary>
        public string FullPath { get; set; }

        /// <summary>文件名（含扩展名）。</summary>
        public string FileName { get; set; }

        /// <summary>字节数。</summary>
        public long Size { get; set; }

        /// <summary>最后修改时间。</summary>
        public DateTime ModifiedTime { get; set; }

        /// <summary>扩展名（小写，含点，如 ".jpg"）。</summary>
        public string Extension { get; set; }

        /// <summary>MD5 哈希（仅在精确验证后填充；未验证为 null）。</summary>
        public string Hash { get; set; }

        /// <summary>本条目的查重置信度。</summary>
        public DuplicateConfidence Confidence { get; set; }

        /// <summary>是否被标记为待删除。</summary>
        public bool MarkedForDelete { get; set; }

        /// <summary>是否被选为保留原件。</summary>
        public bool IsKeepOriginal { get; set; }

        /// <summary>所属重复组的编号（从 1 起，0 表示无组）。</summary>
        public int GroupId { get; set; }

        public FileEntry()
        {
            Confidence = DuplicateConfidence.None;
            GroupId = 0;
        }

        /// <summary>从磁盘文件信息构造条目。</summary>
        public static FileEntry FromPath(string path)
        {
            var info = new FileInfo(path);
            return new FileEntry
            {
                FullPath = info.FullName,
                FileName = info.Name,
                Size = info.Length,
                ModifiedTime = info.LastWriteTime,
                Extension = (info.Extension ?? "").ToLowerInvariant(),
                Confidence = DuplicateConfidence.None,
                GroupId = 0
            };
        }

        /// <summary>供显示用的可读大小。</summary>
        public string SizeText
        {
            get { return FormatSize(Size); }
        }

        /// <summary>供显示用的格式化时间。</summary>
        public string ModifiedTimeText
        {
            get { return ModifiedTime.ToString("yyyy-MM-dd HH:mm:ss"); }
        }

        public static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int u = 0;
            while (size >= 1024 && u < units.Length - 1)
            {
                size /= 1024;
                u++;
            }
            return u == 0
                ? bytes + " B"
                : size.ToString("0.##") + " " + units[u];
        }
    }
}
