using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Common
{
    public static class Parameters
    {
        static Parameters()
        {
            string appPath = FzLib.Program.App.ProgramDirectoryPath;
            if (File.Exists(Path.Combine(appPath, ConfigHere)))
            {
                ConfigPath = "config.json";
                TileConfigPath = "config_tile.json";
                DataPath = "Data";
                TileDownloadPath = "Download";
                BackupPath = "Backup";
                RecordsPath = "Record";
            }
            else if (File.Exists(Path.Combine(appPath, ConfigUp)))
            {
                ConfigPath = "config.json";
                TileConfigPath = "config_tile.json";
                DataPath = "../Data";
                TileDownloadPath = "../Download";
                BackupPath = "../Backup";
                RecordsPath = "../Record";
            }
            else
            {
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                ConfigPath = Path.Combine(folder, AppName, "config.json");
                TileConfigPath = Path.Combine(folder, AppName, "config_tile.json");
                DataPath = Path.Combine(folder, AppName, "Data");
                TileDownloadPath = Path.Combine(folder, AppName, "Download");
                BackupPath = Path.Combine(folder, AppName, "Backup");
                RecordsPath = Path.Combine(folder, AppName, "Record");
            }
        }

        public const string ConfigUp = "CONFIG_UP";
        public const string ConfigHere = "CONFIG_HERE";
        public const string AppName = "MapBoard";
        public const string ClassFieldName = "Key";
        public const string DateFieldName = "Date";
        public const string LabelFieldName = "Info";
        public const string CreateTimeFieldName = "CrtTime";
        public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string DateFormat = "yyyy-MM-dd";

        public static readonly string ConfigPath;
        public static readonly string TileConfigPath;
        public static readonly string DataPath;
        public static readonly string TileDownloadPath;
        public static readonly string BackupPath;
        public static readonly string RecordsPath;

        public static readonly TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.5);
    }
}