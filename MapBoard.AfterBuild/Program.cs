using FzLib.Program;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FzLib.IO.Shortcut;

namespace MapBoard.AfterBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            ShortcutInfo shortcut = new ShortcutInfo();
            shortcut.SetWorkingDirectoryToProgramDirectory();
            shortcut.TargetPath = Path.Combine(App.ProgramDirectoryPath, "Mapboard.exe");

            shortcut.ShortcutFilePath = "地图画板.lnk";
            CreateShortcut(shortcut);

            shortcut.ShortcutFilePath = "地图瓦片下载拼接器.lnk";
            shortcut.Arguments = "tile";
            CreateShortcut(shortcut);

            shortcut.ShortcutFilePath = "GPX工具箱.lnk";
            shortcut.Arguments = "gpx";
            CreateShortcut(shortcut);


            foreach (var folderName in Directory.EnumerateDirectories(App.ProgramDirectoryPath).ToArray())
            {
                DirectoryInfo dir = new DirectoryInfo(folderName);
                if (dir.Name.Length == 2 || dir.Name.Contains("-"))
                {
                    if (!dir.Name.StartsWith("zh"))
                    {
                        dir.Delete(true);
                    }
                }
            }
        }
    }
}
