using System;
using System.Collections.Generic;
using System.IO;
using FileDeduper.Models;
using FileDeduper.Utils;

namespace FileDeduper.Core
{
    /// <summary>
    /// 删除结果：成功删除的文件 + 失败的文件（带原因）。
    /// </summary>
    public class DeleteResult
    {
        public List<string> DeletedPaths;
        public List<DeleteFailure> Failed;

        public DeleteResult()
        {
            DeletedPaths = new List<string>();
            Failed = new List<DeleteFailure>();
        }

        public bool AllSucceeded { get { return Failed.Count == 0; } }
    }

    public class DeleteFailure
    {
        public string Path;
        public string Reason;
    }

    /// <summary>
    /// 文件删除器：支持移入回收站（可恢复）和永久删除两种模式。
    /// </summary>
    public static class FileDeleter
    {
        /// <summary>
        /// 删除一组文件。
        /// </summary>
        public static DeleteResult Delete(List<FileEntry> entries, DeleteMode mode)
        {
            var result = new DeleteResult();
            if (entries == null || entries.Count == 0) return result;

            // 过滤出实际存在且标记删除的路径
            var toDelete = new List<string>();
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (!e.MarkedForDelete) continue;

                if (File.Exists(e.FullPath))
                {
                    toDelete.Add(e.FullPath);
                }
                else
                {
                    result.Failed.Add(new DeleteFailure
                    {
                        Path = e.FullPath,
                        Reason = "文件不存在，未执行删除"
                    });
                }
            }

            if (mode == DeleteMode.Recycle)
            {
                // 先批量回收
                var failed = RecycleBinHelper.SendToRecycleBin(toDelete);
                var failedSet = new HashSet<string>(failed, StringComparer.OrdinalIgnoreCase);

                foreach (var p in toDelete)
                {
                    if (failedSet.Contains(p))
                    {
                        // 回收失败，尝试清除属性后重新回收
                        if (TryClearAttributesAndRetry(p))
                        {
                            result.DeletedPaths.Add(p);
                        }
                        else
                        {
                            result.Failed.Add(new DeleteFailure
                            {
                                Path = p,
                                Reason = "无法移入回收站，未执行永久删除（可能被占用或无权限）"
                            });
                        }
                    }
                    else
                    {
                        result.DeletedPaths.Add(p);
                    }
                }
            }
            else
            {
                // 永久删除：逐个处理以定位失败项
                foreach (var p in toDelete)
                {
                    if (TryPermanentDelete(p))
                    {
                        result.DeletedPaths.Add(p);
                    }
                    else
                    {
                        result.Failed.Add(new DeleteFailure { Path = p, Reason = "无法删除（可能被占用或无权限）" });
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 尝试清除文件属性并重新移入回收站
        /// </summary>
        private static bool TryClearAttributesAndRetry(string path)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(path)) return false;

                // 清除文件属性
                File.SetAttributes(path, FileAttributes.Normal);

                // 尝试逐个回收
                var failed = RecycleBinHelper.SendToRecycleBin(new List<string> { path });
                return failed.Count == 0;
            }
            catch (Exception ex)
            {
                // 记录详细错误信息
                Console.WriteLine("回收失败: " + path + " - " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 尝试永久删除文件
        /// </summary>
        private static bool TryPermanentDelete(string path)
        {
            try
            {
                if (!File.Exists(path)) return true; // 文件不存在，算作删除成功

                // 清除文件属性
                File.SetAttributes(path, FileAttributes.Normal);

                // 尝试删除文件
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                // 记录详细错误信息
                Console.WriteLine("删除失败: " + path + " - " + ex.Message);
                return false;
            }
        }
    }
}
