using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileDeduper.Models;

namespace FileDeduper.Core
{
    /// <summary>
    /// 扫描进度报告。
    /// </summary>
    public class ScanProgress
    {
        public int ScannedCount;
        public int SkippedCount;
        public long TotalBytes;
        public string CurrentDirectory;

        public ScanProgress()
        {
            CurrentDirectory = "";
        }
    }

    /// <summary>
    /// 后台扫描器：遍历多个文件夹收集文件元数据。
    /// 流式枚举，避免内存峰值；通过 IProgress 回报进度，UI 不卡死。
    /// </summary>
    public class FileScanner
    {
        private readonly List<string> _folders;
        private readonly bool _includeSubdirectories;
        private readonly long _minFileSize;

        public FileScanner(List<string> folders, bool includeSubdirectories, long minFileSize)
        {
            _folders = folders ?? new List<string>();
            _includeSubdirectories = includeSubdirectories;
            _minFileSize = minFileSize;
        }

        /// <summary>
        /// 异步扫描。
        /// </summary>
        /// <param name="progress">进度回调（线程安全，自动 marshal 到 UI 线程）。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>所有符合条件的文件条目。</returns>
        public Task<List<FileEntry>> ScanAsync(IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var result = new List<FileEntry>();
                var p = new ScanProgress();
                foreach (string folder in _folders)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (!Directory.Exists(folder)) continue;

                    foreach (string path in EnumerateFilesSafe(folder, p, progress, cancellationToken))
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        try
                        {
                            var info = new FileInfo(path);
                            // 跳过过小文件
                            if (_minFileSize > 0 && info.Length < _minFileSize) continue;

                            var entry = new FileEntry
                            {
                                FullPath = info.FullName,
                                FileName = info.Name,
                                Size = info.Length,
                                ModifiedTime = info.LastWriteTime,
                                Extension = (info.Extension ?? "").ToLowerInvariant(),
                                Confidence = DuplicateConfidence.None,
                                GroupId = 0
                            };
                            result.Add(entry);
                            p.ScannedCount++;
                            p.TotalBytes += entry.Size;
                            string dir = Path.GetDirectoryName(path);
                            if (dir != null) p.CurrentDirectory = dir;

                            // 每扫描一定数量回报一次，避免过于频繁
                            if (p.ScannedCount % 50 == 0 && progress != null)
                            {
                                progress.Report(Clone(p));
                            }
                        }
                        catch
                        {
                            p.SkippedCount++;
                        }
                    }
                    if (progress != null) progress.Report(Clone(p));
                }
                return result;
            }, cancellationToken);
        }

        private IEnumerable<string> EnumerateFilesSafe(
            string folder,
            ScanProgress progressState,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(folder);
            }
            catch
            {
                progressState.SkippedCount++;
                if (progress != null) progress.Report(Clone(progressState));
                yield break;
            }

            foreach (string file in files)
            {
                if (cancellationToken.IsCancellationRequested) yield break;
                yield return file;
            }

            if (!_includeSubdirectories) yield break;

            string[] subdirectories;
            try
            {
                subdirectories = Directory.GetDirectories(folder);
            }
            catch
            {
                progressState.SkippedCount++;
                if (progress != null) progress.Report(Clone(progressState));
                yield break;
            }

            foreach (string subdirectory in subdirectories)
            {
                if (cancellationToken.IsCancellationRequested) yield break;
                foreach (string file in EnumerateFilesSafe(subdirectory, progressState, progress, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) yield break;
                    yield return file;
                }
            }
        }

        private static ScanProgress Clone(ScanProgress p)
        {
            return new ScanProgress
            {
                ScannedCount = p.ScannedCount,
                SkippedCount = p.SkippedCount,
                TotalBytes = p.TotalBytes,
                CurrentDirectory = p.CurrentDirectory
            };
        }
    }
}
