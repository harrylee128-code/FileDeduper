using System;
using System.Collections.Generic;

namespace FileDeduper.Models
{
    /// <summary>
    /// 一组互相重复的文件。
    /// </summary>
    public class DuplicateGroup
    {
        /// <summary>组编号（从 1 起，用于界面显示）。</summary>
        public int Id { get; set; }

        /// <summary>组内所有文件。</summary>
        public List<FileEntry> Files { get; set; }

        /// <summary>整组当前置信度（取组内最低）。</summary>
        public DuplicateConfidence Confidence { get; set; }

        public DuplicateGroup()
        {
            Files = new List<FileEntry>();
            Confidence = DuplicateConfidence.None;
        }

        /// <summary>单个文件大小（组内文件大小相同或应相同）。</summary>
        public long SingleSize
        {
            get { return Files.Count > 0 ? Files[0].Size : 0; }
        }

        /// <summary>若删除所有标记项，可释放的空间（标记项数量 × 单大小）。</summary>
        public long ReclaimableSize
        {
            get
            {
                int del = 0;
                foreach (var f in Files) { if (f.MarkedForDelete) del++; }
                return del * SingleSize;
            }
        }

        /// <summary>组类型（取第一个文件的扩展名）。</summary>
        public string TypeText
        {
            get { return Files.Count > 0 ? (Files[0].Extension == "" ? "(无扩展名)" : Files[0].Extension) : ""; }
        }

        public string ConfidenceText
        {
            get
            {
                switch (Confidence)
                {
                    case DuplicateConfidence.Suspected: return "疑似";
                    case DuplicateConfidence.Likely: return "重复";
                    case DuplicateConfidence.Verified: return "已验证";
                    default: return "未验证";
                }
            }
        }
    }
}
