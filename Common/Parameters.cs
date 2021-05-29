using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Common
{
    public class Parameters
    {
        public const string ConfigUp = "CONFIG_UP";
        public const string ConfigHere = "CONFIG_HERE";
        public const string AppName = "MapBoard";

        static Parameters()
        {
            Instance = new Parameters();
        }

        public Parameters()
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

        public static Parameters Instance { get; }

        public string ConfigPath { get; }
        public string TileConfigPath { get; }
        public string DataPath { get; }
        public string TileDownloadPath { get; }
        public string BackupPath { get; }
        public string RecordsPath { get; }
    }
}