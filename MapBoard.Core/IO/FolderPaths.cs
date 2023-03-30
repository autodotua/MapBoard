﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapBoard.IO
{
    /// <summary>
    /// 路径相关
    /// </summary>
    public static class FolderPaths
    {
        /// <summary>
        /// App名
        /// </summary>
        public const string AppName = "MapBoard";

        /// <summary>
        /// 需要将配置文件放置在程序同级目录下时，在程序目录下新建以该字符串为文件名的空文件
        /// </summary>
        public const string ConfigHere = "CONFIG_HERE";

        /// <summary>
        /// 需要将配置文件放置在程序上级目录下时，在程序目录下新建以该字符串为文件名的空文件
        /// </summary>
        public const string ConfigUp = "CONFIG_UP";

        /// <summary>
        /// 备份目录
        /// </summary>
        public static readonly string BackupPath;

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public static readonly string ConfigPath;

        /// <summary>
        /// 日志目录
        /// </summary>
        public static readonly string LogsPath;

        /// <summary>
        /// GPX录制目录
        /// </summary>
        public static readonly string RecordsPath;

        /// <summary>
        /// 数据目录的根目录
        /// </summary>
        public static readonly string RootDataPath;

        /// <summary>
        /// 瓦片下载目录
        /// </summary>
        public static readonly string TileDownloadPath;

        /// <summary>
        /// 轨迹历史文件路径
        /// </summary>
        public static readonly string TrackHistoryPath;

        /// <summary>
        /// 瓦片缓存目录
        /// </summary>
        public static readonly string TileCachePath;

        static FolderPaths()
        {
            string appPath = FzLib.Program.App.ProgramDirectoryPath;
            if (File.Exists(Path.Combine(appPath, ConfigHere)))
            {
                ConfigPath = "config.json";
                TrackHistoryPath = "tracks.txt";
                RootDataPath = "Data";
                TileDownloadPath = "Download";
                BackupPath = "Backup";
                RecordsPath = "Record";
                LogsPath = "Logs";
                TileCachePath = "Cache/Tiles";
            }
            else if (File.Exists(Path.Combine(appPath, ConfigUp)))
            {
                ConfigPath = "config.json";
                TrackHistoryPath = "tracks.txt";
                RootDataPath = "../Data";
                TileDownloadPath = "../Download";
                BackupPath = "../Backup";
                RecordsPath = "../Record";
                LogsPath = "../Logs";
                TileCachePath = "../Cache/Tiles";
            }
            else
            {
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                ConfigPath = Path.Combine(folder, AppName, "config.json");
                TrackHistoryPath = Path.Combine(folder, AppName, "tracks.txt");
                RootDataPath = Path.Combine(folder, AppName, "Data");
                TileDownloadPath = Path.Combine(folder, AppName, "Download");
                BackupPath = Path.Combine(folder, AppName, "Backup");
                RecordsPath = Path.Combine(folder, AppName, "Record");
                LogsPath = Path.Combine(folder, AppName, "Logs");
                TileCachePath = Path.Combine(folder, AppName, "Cache/Tiles");
            }
            DataPath = GetCurrentDataPath();
        }

        public static string DataPath { get; internal set; }

        /// <summary>
        /// 获取当前的数据目录
        /// </summary>
        /// <returns></returns>
        internal static string GetCurrentDataPath()
        {
            string dataPath;
            if (!Directory.Exists(RootDataPath))
            {
                Directory.CreateDirectory(RootDataPath);
            }

            dataPath = Directory.EnumerateDirectories(RootDataPath)
                .Where(p => DateTime.TryParseExact(Path.GetFileName(p), Parameters.CompactDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
               .OrderByDescending(p => p)
               .FirstOrDefault();
            if (dataPath == null)
            {
                string name = DateTime.Now.ToString(Parameters.CompactDateTimeFormat);
                dataPath = Path.Combine(RootDataPath, name);
                Directory.CreateDirectory(dataPath);
            }
            return dataPath;
        }

        /// <summary>
        /// 从旧的数据目录切换到新数据目录
        /// </summary>
        /// <returns></returns>
        internal static bool SwitchToNewDataPath()
        {
            bool success = true;
            if (!Directory.Exists(RootDataPath))
            {
                Directory.CreateDirectory(RootDataPath);
            }
            else
            {
                var folders = Directory.EnumerateDirectories(RootDataPath)
                .Where(p => DateTime.TryParseExact(Path.GetFileName(p), Parameters.CompactDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
                foreach (var folder in folders)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                    }
                    catch
                    {
                        success = false;
                    }
                }
            }
            string name = DateTime.Now.ToString(Parameters.CompactDateTimeFormat);
            FolderPaths.DataPath = Path.Combine(RootDataPath, name);
            Directory.CreateDirectory(DataPath);
            return success;
        }
    }
}