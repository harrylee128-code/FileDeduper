using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileDeduper.Models;
using FileDeduper.Utils;

namespace FileDeduper.Core
{
    /// <summary>
    /// 两阶段查重核心。
    /// 阶段1 快速预筛（自动执行，极快）：
    ///   - 先按文件大小分组，丢弃 size 唯一的。
    ///   - 同 size 组内细分：
    ///       * 大小+名称+修改时间三项全等 → Likely（重复）
    ///       * 仅大小相等 → Suspected（疑似）
    /// 阶段2 哈希精确确认（按需触发）：对组内文件算 MD5，hash 一致者升格 Verified。
    /// </summary>
    public class DuplicateDetector
    {
        public DuplicateDetector()
        {
        }

        /// <summary>阶段1：快速预筛，返回所有重复组（Likely + Suspected）。</summary>
        public List<DuplicateGroup> FastDetect(List<FileEntry> files)
        {
            var groups = new List<DuplicateGroup>();
            if (files == null || files.Count == 0) return groups;

            // 按大小分组
            var bySize = new Dictionary<long, List<FileEntry>>();
            foreach (var f in files)
            {
                List<FileEntry> bucket;
                if (!bySize.TryGetValue(f.Size, out bucket))
                {
                    bucket = new List<FileEntry>();
                    bySize[f.Size] = bucket;
                }
                bucket.Add(f);
            }

            int groupId = 0;
            foreach (var kv in bySize)
            {
                if (kv.Value.Count < 2) continue; // size 唯一，无重复可能
                groupId = MakeGroups(kv.Value, groups, groupId);
            }
            return groups;
        }

        /// <summary>
        /// 在同一 size 桶内，按 (名称+修改时间) 二次分簇：
        ///   - 同名同时 → Likely 组
        ///   - 其余（仅大小相同）→ 每个独立时间/名称的文件聚成一个 Suspected 组
        /// 为保证 Suspected 组有意义，仅当桶内文件数>=2 时整体算一个疑似组。
        /// </summary>
        private int MakeGroups(List<FileEntry> sameSizeBucket, List<DuplicateGroup> groups, int startId)
        {
            int id = startId;
            // 用「文件名|修改时间Ticks」做键
            var byKey = new Dictionary<string, List<FileEntry>>();
            foreach (var f in sameSizeBucket)
            {
                string key = f.FileName + "|" + f.ModifiedTime.Ticks;
                List<FileEntry> bucket;
                if (!byKey.TryGetValue(key, out bucket))
                {
                    bucket = new List<FileEntry>();
                    byKey[key] = bucket;
                }
                bucket.Add(f);
            }

            foreach (var kv in byKey)
            {
                if (kv.Value.Count >= 2)
                {
                    id++;
                    var g = new DuplicateGroup { Id = id, Confidence = DuplicateConfidence.Likely };
                    foreach (var f in kv.Value)
                    {
                        f.Confidence = DuplicateConfidence.Likely;
                        f.GroupId = id;
                        g.Files.Add(f);
                    }
                    groups.Add(g);
                }
            }

            // 仅大小相同的疑似组：把桶内所有未被 Likely 组收纳的文件合并为一个 Suspected 组
            // 仅当存在 >=2 个这样的文件时才有意义。
            var leftover = new List<FileEntry>();
            foreach (var kv in byKey)
            {
                if (kv.Value.Count < 2) leftover.AddRange(kv.Value);
            }
            if (leftover.Count >= 2)
            {
                id++;
                var g = new DuplicateGroup { Id = id, Confidence = DuplicateConfidence.Suspected };
                foreach (var f in leftover)
                {
                    f.Confidence = DuplicateConfidence.Suspected;
                    f.GroupId = id;
                    g.Files.Add(f);
                }
                groups.Add(g);
            }
            return id;
        }

        /// <summary>
        /// 阶段2：对指定组计算 MD5，按 hash 重新分组，hash 一致者升格 Verified。
        /// 返回新的重复组列表（已验证的组替换原组，未通过哈希的文件被剔除）。
        /// </summary>
        /// <param name="group">待验证的组。</param>
        /// <param name="progress">进度回调(已处理文件数, 总文件数)。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>该组经哈希后产生的子组列表（每个子组内 hash 一致）。</returns>
        public Task<List<DuplicateGroup>> VerifyByHashAsync(
            DuplicateGroup group,
            IProgress<HashVerifyProgress> progress,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var result = new List<DuplicateGroup>();
                if (group == null || group.Files.Count == 0) return result;

                int total = group.Files.Count;
                int done = 0;
                var hp = new HashVerifyProgress { TotalFiles = total, DoneFiles = 0, CurrentFile = "" };

                // 逐文件算哈希
                foreach (var f in group.Files)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    hp.CurrentFile = f.FullPath;
                    if (progress != null) progress.Report(Clone(hp));

                    f.Hash = HashHelper.ComputeFullMd5(f.FullPath, null);
                    done++;
                    hp.DoneFiles = done;
                    if (progress != null) progress.Report(Clone(hp));
                }

                if (cancellationToken.IsCancellationRequested) return result;

                // 按 hash 重新分组（hash 为 null 的单独丢弃，不参与）
                var byHash = new Dictionary<string, List<FileEntry>>();
                foreach (var f in group.Files)
                {
                    if (string.IsNullOrEmpty(f.Hash)) continue;
                    List<FileEntry> bucket;
                    if (!byHash.TryGetValue(f.Hash, out bucket))
                    {
                        bucket = new List<FileEntry>();
                        byHash[f.Hash] = bucket;
                    }
                    bucket.Add(f);
                }

                int newId = 0;
                foreach (var kv in byHash)
                {
                    if (kv.Value.Count < 2) continue; // hash 唯一，非重复
                    newId++;
                    var g = new DuplicateGroup { Id = newId, Confidence = DuplicateConfidence.Verified };
                    foreach (var f in kv.Value)
                    {
                        f.Confidence = DuplicateConfidence.Verified;
                        f.GroupId = newId;
                        g.Files.Add(f);
                    }
                    result.Add(g);
                }
                return result;
            }, cancellationToken);
        }

        private static HashVerifyProgress Clone(HashVerifyProgress p)
        {
            return new HashVerifyProgress
            {
                TotalFiles = p.TotalFiles,
                DoneFiles = p.DoneFiles,
                CurrentFile = p.CurrentFile
            };
        }
    }

    public class HashVerifyProgress
    {
        public int TotalFiles;
        public int DoneFiles;
        public string CurrentFile;

        public HashVerifyProgress()
        {
            CurrentFile = "";
        }
    }
}
