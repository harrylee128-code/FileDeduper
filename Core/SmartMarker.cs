using System;
using System.Collections.Generic;
using System.Linq;
using FileDeduper.Models;

namespace FileDeduper.Core
{
    /// <summary>
    /// 智能标记器：在每组重复文件中按策略挑 1 个保留，其余标记待删除。
    /// 策略可实时切换，切换后对所有组重算。
    /// </summary>
    public static class SmartMarker
    {
        /// <summary>
        /// 对单个组应用策略：选出保留原件，其余标记待删除。
        /// </summary>
        public static void ApplyStrategy(DuplicateGroup group, KeepStrategy strategy)
        {
            if (group == null || group.Files.Count == 0) return;

            if (group.Confidence == DuplicateConfidence.Suspected)
            {
                ClearMarks(group);
                return;
            }

            FileEntry keep = PickKeep(group.Files, strategy);
            foreach (var f in group.Files)
            {
                bool isKeep = (f == keep);
                f.IsKeepOriginal = isKeep;
                f.MarkedForDelete = !isKeep;
            }
        }

        public static void ClearMarks(DuplicateGroup group)
        {
            if (group == null) return;
            foreach (var f in group.Files)
            {
                f.IsKeepOriginal = false;
                f.MarkedForDelete = false;
            }
        }

        /// <summary>对所有组应用同一策略。</summary>
        public static void ApplyStrategy(IEnumerable<DuplicateGroup> groups, KeepStrategy strategy)
        {
            if (groups == null) return;
            foreach (var g in groups)
            {
                ApplyStrategy(g, strategy);
            }
        }

        private static FileEntry PickKeep(List<FileEntry> files, KeepStrategy strategy)
        {
            if (files.Count == 1) return files[0];
            FileEntry best = files[0];
            switch (strategy)
            {
                case KeepStrategy.Oldest:
                    // 保留修改时间最旧的；时间相同则取路径较短者
                    foreach (var f in files)
                    {
                        if (f.ModifiedTime < best.ModifiedTime ||
                            (f.ModifiedTime == best.ModifiedTime && f.FullPath.Length < best.FullPath.Length))
                        {
                            best = f;
                        }
                    }
                    break;
                case KeepStrategy.Newest:
                    foreach (var f in files)
                    {
                        if (f.ModifiedTime > best.ModifiedTime ||
                            (f.ModifiedTime == best.ModifiedTime && f.FullPath.Length < best.FullPath.Length))
                        {
                            best = f;
                        }
                    }
                    break;
                case KeepStrategy.ShortestPath:
                    foreach (var f in files)
                    {
                        if (f.FullPath.Length < best.FullPath.Length) best = f;
                    }
                    break;
            }
            return best;
        }
    }
}
