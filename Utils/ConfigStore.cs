using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileDeduper.Models;

namespace FileDeduper.Utils
{
    /// <summary>
    /// 配置持久化：读写 exe 同目录下的 JSON 文件，实现绿色便携。
    /// 不写注册表、不写 AppData，配置随 exe 一起拷走。
    /// 使用手写的极简 JSON 解析器，避免依赖第三方库或 System.Web.Extensions。
    /// </summary>
    public static class ConfigStore
    {
        /// <summary>配置文件名，固定放在 exe 同目录。</summary>
        public const string ConfigFileName = "FileDeduper.config.json";

        /// <summary>配置文件全路径。</summary>
        public static string ConfigPath
        {
            get
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(dir, ConfigFileName);
            }
        }

        /// <summary>加载配置；文件不存在或解析失败时返回默认配置。</summary>
        public static AppSettings Load()
        {
            var settings = new AppSettings();
            string path = ConfigPath;
            try
            {
                if (!File.Exists(path)) return settings;
                string json = File.ReadAllText(path, Encoding.UTF8);
                var map = (Dictionary<string, object>)MiniJson.Parse(json);
                if (map == null) return settings;

                settings.IncludeSubdirectories = GetBool(map, "IncludeSubdirectories", settings.IncludeSubdirectories);
                settings.DeleteMode = (DeleteMode)GetLong(map, "DeleteMode", (long)settings.DeleteMode);
                settings.KeepStrategy = (KeepStrategy)GetLong(map, "KeepStrategy", (long)settings.KeepStrategy);
                settings.HashVerifyLikelyGroups = GetBool(map, "HashVerifyLikelyGroups", settings.HashVerifyLikelyGroups);
                settings.HardwareAccelerationMode = (HardwareAccelerationMode)GetLong(map, "HardwareAccelerationMode", (long)settings.HardwareAccelerationMode);
                settings.MinFileSize = GetLong(map, "MinFileSize", settings.MinFileSize);

                object foldersObj;
                if (map.TryGetValue("LastFolders", out foldersObj) && foldersObj is List<object>)
                {
                    var folders = (List<object>)foldersObj;
                    settings.LastFolders = new List<string>();
                    foreach (var f in folders)
                    {
                        if (f is string) settings.LastFolders.Add((string)f);
                    }
                }
            }
            catch
            {
                // 配置损坏不影响启动，回退默认。
            }
            return settings;
        }

        /// <summary>保存配置到 exe 同目录。</summary>
        public static void Save(AppSettings settings)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\r\n");
                sb.Append("  \"IncludeSubdirectories\": ").Append(settings.IncludeSubdirectories ? "true" : "false").Append(",\r\n");
                sb.Append("  \"DeleteMode\": ").Append((int)settings.DeleteMode).Append(",\r\n");
                sb.Append("  \"KeepStrategy\": ").Append((int)settings.KeepStrategy).Append(",\r\n");
                sb.Append("  \"HashVerifyLikelyGroups\": ").Append(settings.HashVerifyLikelyGroups ? "true" : "false").Append(",\r\n");
                sb.Append("  \"HardwareAccelerationMode\": ").Append((int)settings.HardwareAccelerationMode).Append(",\r\n");
                sb.Append("  \"MinFileSize\": ").Append(settings.MinFileSize).Append(",\r\n");
                sb.Append("  \"LastFolders\": [");
                for (int i = 0; i < settings.LastFolders.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(MiniJson.Quote(settings.LastFolders[i]));
                }
                sb.Append("]\r\n");
                sb.Append("}\r\n");
                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // 保存失败静默处理，避免界面崩溃。
            }
        }

        // ---- 强类型取值辅助（兼容 JSON 里数字可能是 double 或 long）----
        private static bool GetBool(Dictionary<string, object> map, string key, bool defaultValue)
        {
            object v;
            if (map.TryGetValue(key, out v) && v is bool) return (bool)v;
            return defaultValue;
        }

        private static long GetLong(Dictionary<string, object> map, string key, long defaultValue)
        {
            object v;
            if (!map.TryGetValue(key, out v) || v == null) return defaultValue;
            if (v is long) return (long)v;
            if (v is double) return (long)(double)v;
            if (v is int) return (int)v;
            return defaultValue;
        }
    }
}
