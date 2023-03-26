using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapBoard.IO
{
    public static class FolderPaths
    {
        public const string AppName = "MapBoard";

        public const string ConfigHere = "CONFIG_HERE";

        public const string ConfigUp = "CONFIG_UP";

        public static readonly string BackupPath;

        public static readonly string ConfigPath;

        public static readonly string LogsPath;

        public static readonly string RecordsPath;

        public static readonly string RootDataPath;

        public static readonly string TileDownloadPath;

        public static readonly string TrackHistoryPath;

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