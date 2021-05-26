//#define TEST

using FzLib.IO;
using FzLib.Program;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using static FzLib.IO.Shortcut;

namespace MapBoard.AfterBuild
{
    internal class Program
    {
        private const string appPath = "App";

        private static void Main(string[] args)
        {
#if !DEBUG || TEST
            DeleteUselessFolders();
            Console.WriteLine("无用目录删除完成");
            CreateConfig();
            Console.WriteLine("配置文件创建完成");
            CreateShortcuts();
            Console.WriteLine("快捷方式创建完成");
            if (Path.GetFileName(App.ProgramDirectoryPath) != "App")
            {
                MoveFiles();
                Console.WriteLine("文件和文件夹移动完成");
            }
            Thread.Sleep(1000);
#endif
        }

        private static void CreateConfig()
        {
            File.WriteAllText("config_tile.json",
                JsonConvert.SerializeObject(new
                {
                    UrlCollection = new
                    {
                        Sources = new dynamic[] {
            new   {
        Name= "高德地图",
        Url= "http://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&scl=1&style=8&x={x}&y={y}&z={z}"
      },
     new {
        Name= "谷歌卫星",
        Url= "http://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={z}"
      },
     new {
        Name= "谷歌卫星中国（GCJ02）",
        Url= "http://mt1.google.cn/vt/lyrs=s&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}"
      },
    new  {
        Name= "谷歌卫星中国（WGS84）",
        Url= "http://mt1.google.cn/vt/lyrs=s&x={x}&y={y}&z={z}"
      },
  new    {
        Name= "天地图",
        Url= "http://t0.tianditu.com/vec_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=vec&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=4cb121d316f53f85357887949e827fd4"
      },
   new   {
        Name= "天地图注记",
        Url= "http://t0.tianditu.com/cva_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=cva&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=4cb121d316f53f85357887949e827fd4"
      }
                    }
                    }
                })); File.WriteAllText("config.json",
            JsonConvert.SerializeObject(new
            {
                BaseLayers = new dynamic[] {
            new   {
   Type="WebTiledLayer",
      Path= "https://mt1.google.cn/vt/lyrs=s&x={x}&y={y}&z={z}",
      Enable= true      }
                }
            }));
        }

        private static void CreateShortcuts()
        {
            ShortcutInfo shortcut = new ShortcutInfo();
            shortcut.SetWorkingDirectoryToProgramDirectory();
            if (Path.GetFileName(App.ProgramDirectoryPath) == "App")
            {
                shortcut.TargetPath = Path.Combine(App.ProgramDirectoryPath, "Mapboard.exe");
            }
            else
            {
                shortcut.TargetPath = Path.Combine(App.ProgramDirectoryPath, appPath, "Mapboard.exe");
            }
            shortcut.ShortcutFilePath = GetPath("地图画板.lnk");
            CreateShortcut(shortcut);

            shortcut.ShortcutFilePath = GetPath("地图瓦片下载拼接器.lnk");
            shortcut.Arguments = "tile";
            CreateShortcut(shortcut);

            shortcut.ShortcutFilePath = GetPath("GPX工具箱.lnk");
            shortcut.Arguments = "gpx";
            CreateShortcut(shortcut);

            string GetPath(string name)
            {
                if (Path.GetFileName(App.ProgramDirectoryPath) == "App")
                {
                    return Path.Combine(Path.GetDirectoryName(App.ProgramDirectoryPath), name);
                }
                return Path.Combine(App.ProgramDirectoryPath, name);
            }
        }

        private static void DeleteUselessFolders()
        {
            foreach (var dir in from folderName in Directory.EnumerateDirectories(App.ProgramDirectoryPath).ToArray()
                                let dir = new DirectoryInfo(folderName)
                                where dir.Name.Length == 2 || dir.Name.Contains("-")
                                where !dir.Name.StartsWith("zh")
                                select dir)
            {
                dir.Delete(true);
            }
        }

        public static void MoveFiles()
        {
            if (!Directory.Exists(Path.Combine(App.ProgramDirectoryPath, appPath)))
            {
                Directory.CreateDirectory(Path.Combine(App.ProgramDirectoryPath, appPath));
            }

            foreach (var dir in Directory.EnumerateDirectories(App.ProgramDirectoryPath)
                .Where(p => !(new string[] { "Data", appPath, "Download" }).Contains(Path.GetFileName(p)))
                .ToArray())
            {
                string target = Path.Combine(App.ProgramDirectoryPath, appPath, Path.GetFileName(dir));
                if (Directory.Exists(target))
                {
                    Directory.Delete(target, true);
                }
                Directory.Move(dir, target);
            }
            foreach (var file in Directory.EnumerateFiles(App.ProgramDirectoryPath)
                .Where(p => !p.EndsWith(".lnk")).ToArray())
            {
                string target = Path.Combine(App.ProgramDirectoryPath, appPath, Path.GetFileName(file));
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                File.Move(file, target);
            }
        }
    }
}