using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using FzLib.Program;
using MapBoard.Common;
using MapBoard.Main.Style;
using MapBoard.Main.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FzLib.Basic.Loop;

namespace MapBoard.Main.IO
{
    public static class Package
    {
        /// <summary>
        /// 导入地图包
        /// </summary>
        /// <param name="path"></param>
        public static void ImportMap(string path)
        {
            StyleCollection.Instance.Styles.Clear();
            if (Directory.Exists(Config.DataPath))
            {
                Directory.Delete(Config.DataPath, true);
            }
            ZipFile.ExtractToDirectory(path, Config.DataPath);

            //Information.Restart();
            StyleCollection.ResetStyles();
        }
        /// <summary>
        /// 导入图层包
        /// </summary>
        /// <param name="path"></param>
        public static void ImportLayer(string path)
        {
            string tempDirectoryPath = Path.Combine(Config.DataPath, "temp");
            if (Directory.Exists(tempDirectoryPath))
            {
                Directory.Delete(tempDirectoryPath, true);
            }
            Directory.CreateDirectory(tempDirectoryPath);

            ZipFile.ExtractToDirectory(path, tempDirectoryPath);

            StyleInfo style = Newtonsoft.Json.JsonConvert.DeserializeObject<StyleInfo>(File.ReadAllText(Path.Combine(tempDirectoryPath, "style.json")));
            var files = Shapefile.GetExistShapefiles(tempDirectoryPath, style.Name);
                
            //    Directory.EnumerateFiles(tempDirectoryPath).Where(p => Path.GetFileNameWithoutExtension(p) == style.Name).ToArray();
            //if(files.Length<3)
            //{
            //    throw new Exception("缺少文件");
            //}
            List<string> copyedFiles = new List<string>();
            foreach (var file in files)
            {
                string target = Path.Combine(Config.DataPath, Path.GetFileName(file));
                if(File.Exists(target))
                {
                    copyedFiles.ForEach(p => File.Delete(p));
                    throw new IOException($"文件{target}已存在");
                }
                File.Move(file, target);
                copyedFiles.Add(target);
            }

            StyleCollection.Instance.Styles.Add(style);
        }
        /// <summary>
        /// 导出底涂包，实则是zip文件
        /// </summary>
        /// <param name="path"></param>
        public static void ExportMap(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            StyleCollection.Instance.Save();
            var styles = StyleCollection.Instance.Styles.ToArray();
            StyleCollection.Instance.SaveWhenChanged = false;
            StyleCollection.Instance.Styles.Clear();
            styles.ForEach(p => p.Table = null);
            ZipFile.CreateFromDirectory(Config.DataPath, path);
            styles.ForEach(p => StyleCollection.Instance.Styles.Add(p));
            StyleCollection.Instance.SaveWhenChanged = true;
        }
        /// <summary>
        /// 导出图层包，实则是zip文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="style"></param>
        public static void ExportLayer(string path, StyleInfo style)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            int index = StyleCollection.Instance.Styles.IndexOf(style);
            StyleCollection.Instance.SaveWhenChanged = false;
            StyleCollection.Instance.Styles.Remove(style);
            style.Table = null;

            string tempDirectoryPath = Path.Combine(Config.DataPath, "temp");
            if (Directory.Exists(tempDirectoryPath))
            {
                Directory.Delete(tempDirectoryPath, true);
            }
            Directory.CreateDirectory(tempDirectoryPath);

            foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath,style.Name))
            {
                File.Copy(file, Path.Combine(tempDirectoryPath, Path.GetFileName(file)));
            }
            File.WriteAllText(Path.Combine(tempDirectoryPath, "style.json"), Newtonsoft.Json.JsonConvert.SerializeObject(style));

            ZipFile.CreateFromDirectory(tempDirectoryPath, path);
            StyleCollection.Instance.Styles.Insert(index, style);
            StyleCollection.Instance.SaveWhenChanged = true;
            Directory.Delete(tempDirectoryPath, true);
        }
    }
}
