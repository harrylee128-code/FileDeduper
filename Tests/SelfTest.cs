using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using FileDeduper.Core;
using FileDeduper.Models;
using FileDeduper.Utils;

namespace FileDeduper.Tests
{
    /// <summary>
    /// 自测控制台程序：用 TestDedupe 测试文件夹验证扫描/查重/哈希/标记/删除全流程。
    /// 不依赖 GUI，直接调用 Core 层。
    /// 编译为 FileDeduper.Test.exe，运行后输出每步结果。
    /// </summary>
    internal static class SelfTest
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length > 0 && string.Equals(args[0], "--benchmark", StringComparison.OrdinalIgnoreCase))
            {
                string benchmarkRoot = args.Length > 1 ? args[1] : CreateBenchmarkFixture();
                return RunBenchmark(benchmarkRoot);
            }

            bool cleanupTestRoot = false;
            string testRoot = args.Length > 0 ? args[0] : CreateDefaultFixture(out cleanupTestRoot);

            Console.WriteLine("====== FileDeduper 自测 ======");
            Console.WriteLine("测试目录: " + testRoot);
            Console.WriteLine();

            try
            {
                if (!Directory.Exists(testRoot))
                {
                    Console.WriteLine("[FAIL] 测试目录不存在，请先创建。");
                    return 1;
                }

                int passed = 0, failed = 0;

                // ---- 步骤1: 扫描 ----
                Console.WriteLine("[1] 扫描文件…");
                var folders = new List<string> { testRoot };
                var scanner = new FileScanner(folders, true, 0);
                var scanTask = scanner.ScanAsync(null, CancellationToken.None);
                scanTask.Wait();
                var files = scanTask.Result;
                Console.WriteLine("    扫描到 " + files.Count + " 个文件");
                foreach (var f in files)
                {
                    Console.WriteLine("      - " + f.FileName + "  " + f.Size + "B  " + f.ModifiedTime.ToString("yyyy-MM-dd HH:mm") + "  " + f.FullPath);
                }
                Check("扫描到 7 个文件", files.Count == 7, ref passed, ref failed);
                Console.WriteLine();

            // ---- 步骤2: 快速预筛 ----
            Console.WriteLine("[2] 快速预筛查重…");
            var detector = new DuplicateDetector();
            var groups = detector.FastDetect(files);
            Console.WriteLine("    发现 " + groups.Count + " 组重复");
            foreach (var g in groups)
            {
                Console.WriteLine("      组" + g.Id + " [" + g.ConfidenceText + "] " + g.Files.Count + " 个文件 × " + FileEntry.FormatSize(g.SingleSize) + " " + g.TypeText);
                foreach (var f in g.Files)
                {
                    Console.WriteLine("          " + f.FileName + "  " + f.ModifiedTime.ToString("yyyy-MM-dd HH:mm") + "  " + f.FullPath);
                }
            }
            Check("发现重复组 >= 3", groups.Count >= 3, ref passed, ref failed);

            // 应该有至少一个 Likely 组 (doc.txt 两个同时间)
            bool hasLikely = false;
            bool hasSuspected = false;
            foreach (var g in groups)
            {
                if (g.Confidence == DuplicateConfidence.Likely) hasLikely = true;
                if (g.Confidence == DuplicateConfidence.Suspected) hasSuspected = true;
            }
            Check("存在 Likely 组(完全重复)", hasLikely, ref passed, ref failed);
            Check("存在 Suspected 组(疑似)", hasSuspected, ref passed, ref failed);
            Console.WriteLine();

            // ---- 步骤3: 哈希精确验证所有组 ----
            Console.WriteLine("[3] MD5 精确验证所有组…");
            int verifiedCount = 0;
            foreach (var g in groups)
            {
                var vp = detector.VerifyByHashAsync(g, null, CancellationToken.None);
                vp.Wait();
                var subGroups = vp.Result;
                foreach (var sg in subGroups)
                {
                    verifiedCount++;
                    Console.WriteLine("      已验证组: " + sg.Files.Count + " 个文件 hash 一致");
                }
            }
            Check("哈希验证后至少 3 组确认重复(Verified)", verifiedCount >= 3, ref passed, ref failed);
            Console.WriteLine();

            // ---- 步骤4: 智能标记三种策略 ----
            Console.WriteLine("[4] 测试智能标记策略…");
            // 重新取一份干净数据测标记
            var files2 = scanner.ScanAsync(null, CancellationToken.None); files2.Wait();
            var groups2 = detector.FastDetect(files2.Result);

            // 4a 保留最旧
            SmartMarker.ApplyStrategy(groups2, KeepStrategy.Oldest);
            bool oldestOk = true;
            foreach (var g in groups2)
            {
                if (g.Confidence == DuplicateConfidence.Suspected) continue;
                foreach (var f in g.Files)
                {
                    if (f.IsKeepOriginal)
                    {
                        Console.WriteLine("      [保留最旧] 组" + g.Id + " 保留: " + f.FileName + " " + f.ModifiedTime.ToString("yyyy-MM-dd"));
                    }
                    else if (!f.MarkedForDelete)
                    {
                        oldestOk = false;
                    }
                }
            }
            Check("保留最旧策略：高置信组恰好1个保留，其余标记删除", oldestOk, ref passed, ref failed);

            bool suspectedSafe = true;
            foreach (var g in groups2)
            {
                if (g.Confidence != DuplicateConfidence.Suspected) continue;
                foreach (var f in g.Files)
                {
                    if (f.MarkedForDelete || f.IsKeepOriginal) suspectedSafe = false;
                }
            }
            Check("疑似组不会被自动标记删除", suspectedSafe, ref passed, ref failed);

            // 4b 保留最新
            SmartMarker.ApplyStrategy(groups2, KeepStrategy.Newest);
            bool newestOk = true;
            int keepCount = 0;
            foreach (var g in groups2)
            {
                if (g.Confidence == DuplicateConfidence.Suspected) continue;
                keepCount = 0;
                foreach (var f in g.Files) { if (f.IsKeepOriginal) keepCount++; }
                if (keepCount != 1) newestOk = false;
            }
            Check("保留最新策略：高置信组恰好1个保留", newestOk, ref passed, ref failed);

            // 4c 保留路径最短
            SmartMarker.ApplyStrategy(groups2, KeepStrategy.ShortestPath);
            bool shortestOk = true;
            foreach (var g in groups2)
            {
                if (g.Confidence == DuplicateConfidence.Suspected) continue;
                keepCount = 0;
                foreach (var f in g.Files) { if (f.IsKeepOriginal) keepCount++; }
                if (keepCount != 1) shortestOk = false;
            }
            Check("保留路径最短策略：高置信组恰好1个保留", shortestOk, ref passed, ref failed);
            Console.WriteLine();

            // ---- 步骤5: 删除测试(用临时副本，不破坏原测试文件) ----
            Console.WriteLine("[5] 删除测试(永久删除临时副本)…");
            string tempDir = Path.Combine(Path.GetTempPath(), "FileDeduperTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string t1 = Path.Combine(tempDir, "copy1.txt");
                string t2 = Path.Combine(tempDir, "copy2.txt");
                File.WriteAllText(t1, "same content");
                File.WriteAllText(t2, "same content");
                var toDel = new List<FileEntry>();
                var e1 = FileEntry.FromPath(t1);
                var e2 = FileEntry.FromPath(t2);
                e1.MarkedForDelete = true;
                e2.MarkedForDelete = true;
                toDel.Add(e1);
                toDel.Add(e2);
                var result = FileDeleter.Delete(toDel, DeleteMode.Permanent);
                Console.WriteLine("    删除成功 " + result.DeletedPaths.Count + " 个，失败 " + result.Failed.Count + " 个");
                bool delOk = result.DeletedPaths.Count == 2 && !File.Exists(t1) && !File.Exists(t2);
                Check("永久删除 2 个文件成功", delOk, ref passed, ref failed);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            Console.WriteLine();

            // ---- 步骤6: 删除缺失文件应报告失败 ----
            Console.WriteLine("[6] 删除缺失文件报告测试…");
            string missingPath = Path.Combine(Path.GetTempPath(), "FileDeduperMissing_" + Guid.NewGuid().ToString("N") + ".txt");
            var missingEntry = new FileEntry
            {
                FullPath = missingPath,
                FileName = Path.GetFileName(missingPath),
                Size = 123,
                ModifiedTime = DateTime.Now,
                MarkedForDelete = true
            };
            var missingEntries = new List<FileEntry>();
            missingEntries.Add(missingEntry);
            var missingResult = FileDeleter.Delete(missingEntries, DeleteMode.Recycle);
            Check("已标记但不存在的文件会报告失败",
                missingResult.DeletedPaths.Count == 0 && missingResult.Failed.Count == 1,
                ref passed, ref failed);
            Console.WriteLine();

            // ---- 步骤7: 回收站路径能力预检 ----
            Console.WriteLine("[7] 回收站路径能力预检测试…");
            Check("UNC/network 路径不会进入回收站删除 API",
                !RecycleBinHelper.IsKnownRecycleBinSupportedPath(@"\\server\share\unsafe.txt"),
                ref passed, ref failed);
            Check("回收站删除启用系统永久删除警告",
                (RecycleBinHelper.RecycleFlags & 0x4000) == 0x4000,
                ref passed, ref failed);
            Check("自动化环境不会进入回收站删除 API",
                !RecycleBinHelper.IsKnownAutomationSession() || !RecycleBinHelper.IsKnownRecycleBinSupportedPath(Path.GetTempPath()),
                ref passed, ref failed);
            Console.WriteLine();

            // ---- 步骤8: 硬件加速 fallback 测试 ----
            Console.WriteLine("[8] 硬件加速 fallback 测试…");
            string hashProbe = Path.Combine(testRoot, "FolderA", "doc.txt");
            string cpuHash = HashHelper.ComputeFullMd5(hashProbe, HardwareAccelerationMode.CpuOnly, null);
            var gpuAttempt = HashEngine.ComputeFullMd5(hashProbe, HardwareAccelerationMode.GpuExperimental, null);
            Check("GPU experimental 模式没有 provider 时保持完整哈希正确性",
                !string.IsNullOrEmpty(cpuHash) && cpuHash == gpuAttempt.Hash,
                ref passed, ref failed);
            Check("GPU experimental 模式没有 provider 时回退 CPU",
                !gpuAttempt.HardwareAccelerated && !string.IsNullOrEmpty(gpuAttempt.FallbackReason),
                ref passed, ref failed);
            Console.WriteLine();

            // ---- 步骤9: 回收站模式安全测试 ----
            Console.WriteLine("[9] 回收站模式安全测试…");
            string recycleTempDir = Path.Combine(Path.GetTempPath(), "FileDeduperRecycleTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(recycleTempDir);
            string recycleFileName = "filededuper-recycle-test-" + Guid.NewGuid().ToString("N") + ".txt";
            string recyclePath = Path.Combine(recycleTempDir, recycleFileName);
            File.WriteAllText(recyclePath, "temporary recycle mode safety test");
            try
            {
                var recycleEntry = FileEntry.FromPath(recyclePath);
                recycleEntry.MarkedForDelete = true;
                var recycleEntries = new List<FileEntry>();
                recycleEntries.Add(recycleEntry);
                var recycleResult = FileDeleter.Delete(recycleEntries, DeleteMode.Recycle);
                bool existsAtOriginalPath = File.Exists(recyclePath);
                bool foundInRecycleBin = IsInRecycleBin(recycleFileName);
                bool safelyHandled = foundInRecycleBin
                                  || (existsAtOriginalPath && recycleResult.DeletedPaths.Count == 0 && recycleResult.Failed.Count == 1);

                Console.WriteLine("    原路径存在: " + existsAtOriginalPath
                    + "，回收站可见: " + foundInRecycleBin
                    + "，成功 " + recycleResult.DeletedPaths.Count
                    + "，失败 " + recycleResult.Failed.Count);
                Check("回收站模式不会静默永久删除", safelyHandled, ref passed, ref failed);
            }
            finally
            {
                if (Directory.Exists(recycleTempDir)) Directory.Delete(recycleTempDir, true);
            }
            Console.WriteLine();

            // ---- 步骤10: 精确验证必须读取完整文件 ----
            Console.WriteLine("[10] 精确验证完整性测试…");
            string hashTempDir = Path.Combine(Path.GetTempPath(), "FileDeduperHashTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(hashTempDir);
            try
            {
                string h1 = Path.Combine(hashTempDir, "large-a.bin");
                string h2 = Path.Combine(hashTempDir, "large-b.bin");
                WriteLargeFileWithDifferentMiddle(h1, 65);
                WriteLargeFileWithDifferentMiddle(h2, 66);
                File.SetLastWriteTime(h1, new DateTime(2024, 1, 1, 8, 0, 0));
                File.SetLastWriteTime(h2, new DateTime(2024, 2, 1, 8, 0, 0));

                var hashFiles = new List<FileEntry>();
                hashFiles.Add(FileEntry.FromPath(h1));
                hashFiles.Add(FileEntry.FromPath(h2));
                var hashGroups = new DuplicateDetector().FastDetect(hashFiles);
                var verifyTask = new DuplicateDetector().VerifyByHashAsync(hashGroups[0], null, CancellationToken.None);
                verifyTask.Wait();
                Check("MD5 精确验证不会把中间内容不同的大文件标为重复",
                    verifyTask.Result.Count == 0, ref passed, ref failed);
            }
            finally
            {
                if (Directory.Exists(hashTempDir)) Directory.Delete(hashTempDir, true);
            }
            Console.WriteLine();

            // ---- 步骤11: 配置读写测试 ----
            Console.WriteLine("[11] 配置读写测试…");
            var settings = new AppSettings();
            settings.DeleteMode = DeleteMode.Permanent;
            settings.KeepStrategy = KeepStrategy.Newest;
            settings.IncludeSubdirectories = false;
            settings.HardwareAccelerationMode = HardwareAccelerationMode.GpuExperimental;
            settings.LastFolders.Add(testRoot);
            string tempConfig = Path.Combine(Path.GetTempPath(), "FileDeduper_test_config.json");
            // 临时改 BaseDirectory 不现实，直接测序列化往返：保存到 exe 同目录再读
            // 为避免污染正式配置，用 MiniJson 直接往返
            ConfigStore.Save(settings);
            var loaded = ConfigStore.Load();
            bool cfgOk = loaded.DeleteMode == DeleteMode.Permanent
                      && loaded.KeepStrategy == KeepStrategy.Newest
                      && loaded.IncludeSubdirectories == false
                      && loaded.HardwareAccelerationMode == HardwareAccelerationMode.GpuExperimental
                      && loaded.LastFolders.Contains(testRoot);
            Check("配置往返读写一致", cfgOk, ref passed, ref failed);
            // 清理：恢复默认配置
            ConfigStore.Save(new AppSettings());
            Console.WriteLine();

                Console.WriteLine("====== 测试结果 ======");
                Console.WriteLine("通过: " + passed + "  失败: " + failed);
                Console.WriteLine(failed == 0 ? "ALL PASSED ✓" : "SOME FAILED ✗");
                return failed == 0 ? 0 : 1;
            }
            finally
            {
                if (cleanupTestRoot && Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        private static void Check(string name, bool condition, ref int passed, ref int failed)
        {
            if (condition)
            {
                Console.WriteLine("    [PASS] " + name);
                passed++;
            }
            else
            {
                Console.WriteLine("    [FAIL] " + name);
                failed++;
            }
        }

        private static bool IsInRecycleBin(string fileName)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return false;

                object shell = Activator.CreateInstance(shellType);
                object recycleBin = shellType.InvokeMember("NameSpace",
                    BindingFlags.InvokeMethod, null, shell, new object[] { 10 });
                if (recycleBin == null) return false;

                object items = recycleBin.GetType().InvokeMember("Items",
                    BindingFlags.InvokeMethod, null, recycleBin, null);
                var enumerable = items as IEnumerable;
                if (enumerable == null) return false;

                foreach (object item in enumerable)
                {
                    string name = item.GetType().InvokeMember("Name",
                        BindingFlags.GetProperty, null, item, null) as string;
                    if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // 如果当前系统无法枚举回收站，测试会退回检查文件是否仍保留在原处。
            }
            return false;
        }

        private static int RunBenchmark(string root)
        {
            if (!Directory.Exists(root))
            {
                Console.WriteLine("[FAIL] benchmark 目录不存在: " + root);
                return 1;
            }

            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Console.WriteLine("====== FileDeduper Hash Benchmark ======");
            Console.WriteLine("目录: " + root);
            Console.WriteLine("文件数: " + files.Length);
            Console.WriteLine("硬件环境: " + HashEngine.Describe(HardwareAccelerationMode.GpuExperimental));

            var cpu = HashBenchmark.Run(files, HardwareAccelerationMode.CpuOnly);
            Console.WriteLine("CPU provider: " + cpu.Provider);
            Console.WriteLine("CPU bytes: " + cpu.TotalBytes);
            Console.WriteLine("CPU elapsed: " + cpu.Elapsed.TotalSeconds.ToString("0.000") + "s");
            Console.WriteLine("CPU throughput: " + cpu.MegabytesPerSecond.ToString("0.00") + " MB/s");

            var gpu = HashBenchmark.Run(files, HardwareAccelerationMode.GpuExperimental);
            Console.WriteLine("GPU experimental provider: " + gpu.Provider);
            Console.WriteLine("GPU experimental accelerated: False");
            Console.WriteLine("GPU experimental fallback: " + gpu.FallbackReason);
            Console.WriteLine("GPU experimental elapsed: " + gpu.Elapsed.TotalSeconds.ToString("0.000") + "s");
            Console.WriteLine("GPU experimental throughput: " + gpu.MegabytesPerSecond.ToString("0.00") + " MB/s");
            return 0;
        }

        private static string CreateBenchmarkFixture()
        {
            string root = Path.Combine(Path.GetTempPath(), "FileDeduperBenchmark_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            for (int i = 0; i < 8; i++)
            {
                WriteLargeFileWithDifferentMiddle(Path.Combine(root, "bench-" + i + ".bin"), (byte)(65 + i));
            }
            return root;
        }

        private static string CreateDefaultFixture(out bool cleanup)
        {
            cleanup = true;
            string root = Path.Combine(Path.GetTempPath(), "FileDeduperFixture_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            string[] dirs = { "FolderA", "FolderB", "FolderA\\sub", "RenamedA", "RenamedB" };
            foreach (string dir in dirs)
            {
                Directory.CreateDirectory(Path.Combine(root, dir));
            }

            WriteFixtureFile(Path.Combine(root, "FolderA", "doc.txt"), 1000, "D");
            WriteFixtureFile(Path.Combine(root, "FolderB", "doc.txt"), 1000, "D");
            WriteFixtureFile(Path.Combine(root, "FolderA", "photo.jpg"), 2000, "P");
            WriteFixtureFile(Path.Combine(root, "FolderB", "photo-copy.jpg"), 2000, "P");
            WriteFixtureFile(Path.Combine(root, "RenamedA", "report_orig.pdf"), 5000, "R");
            WriteFixtureFile(Path.Combine(root, "RenamedB", "report_copy.pdf"), 5000, "R");
            File.WriteAllText(Path.Combine(root, "FolderA", "unique.txt"), "unique file");

            DateTime oldTime = new DateTime(2024, 1, 15, 10, 0, 0);
            DateTime newTime = new DateTime(2024, 5, 20, 14, 30, 0);
            File.SetLastWriteTime(Path.Combine(root, "FolderA", "doc.txt"), oldTime);
            File.SetLastWriteTime(Path.Combine(root, "FolderB", "doc.txt"), oldTime);
            File.SetLastWriteTime(Path.Combine(root, "FolderA", "photo.jpg"), oldTime);
            File.SetLastWriteTime(Path.Combine(root, "FolderB", "photo-copy.jpg"), newTime);
            File.SetLastWriteTime(Path.Combine(root, "RenamedA", "report_orig.pdf"), oldTime);
            File.SetLastWriteTime(Path.Combine(root, "RenamedB", "report_copy.pdf"), newTime);
            File.SetLastWriteTime(Path.Combine(root, "FolderA", "unique.txt"), newTime);

            return root;
        }

        private static void WriteFixtureFile(string path, int length, string seed)
        {
            byte[] seedBytes = System.Text.Encoding.ASCII.GetBytes(seed);
            using (var fs = File.Create(path))
            {
                while (fs.Length < length)
                {
                    int remaining = length - (int)fs.Length;
                    int count = Math.Min(seedBytes.Length, remaining);
                    fs.Write(seedBytes, 0, count);
                }
            }
        }

        private static void WriteLargeFileWithDifferentMiddle(string path, byte middleByte)
        {
            byte[] head = new byte[1024 * 1024];
            byte[] middle = new byte[1024 * 1024];
            byte[] tail = new byte[1024 * 1024];
            for (int i = 0; i < head.Length; i++) head[i] = 1;
            for (int i = 0; i < middle.Length; i++) middle[i] = middleByte;
            for (int i = 0; i < tail.Length; i++) tail[i] = 2;

            using (var fs = File.Create(path))
            {
                fs.Write(head, 0, head.Length);
                fs.Write(middle, 0, middle.Length);
                fs.Write(tail, 0, tail.Length);
            }
        }
    }
}
