using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using FzLib.Program;
using MapBoard.Style;
using MapBoard.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.IO
{
    public static class Mbpkg
    {
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
            var files = Directory.EnumerateFiles(tempDirectoryPath).Where(p => Path.GetFileNameWithoutExtension(p) == style.Name).ToArray();
            if(files.Length<3)
            {
                throw new Exception("缺少文件");
            }
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
        public static void ExportMap(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            StyleCollection.Instance.Save();
            var styles = StyleCollection.Instance.Styles.ToArray();
            StyleCollection.Instance.Styles.Clear();
            styles.ForEach(p => p.Table = null);
            ZipFile.CreateFromDirectory(Config.DataPath, path);
            styles.ForEach(p => StyleCollection.Instance.Styles.Add(p));
        }
        public static void ExportLayer(string path, StyleInfo style)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            int index = StyleCollection.Instance.Styles.IndexOf(style);
            StyleCollection.Instance.Styles.Remove(style);
            style.Table = null;

            string tempDirectoryPath = Path.Combine(Config.DataPath, "temp");
            if (Directory.Exists(tempDirectoryPath))
            {
                Directory.Delete(tempDirectoryPath, true);
            }
            Directory.CreateDirectory(tempDirectoryPath);

            foreach (var file in Directory.EnumerateFiles(Config.DataPath).Where(p => Path.GetFileNameWithoutExtension(p) == style.Name))
            {
                File.Copy(file, Path.Combine(tempDirectoryPath, Path.GetFileName(file)));
            }
            File.WriteAllText(Path.Combine(tempDirectoryPath, "style.json"), Newtonsoft.Json.JsonConvert.SerializeObject(style));

            ZipFile.CreateFromDirectory(tempDirectoryPath, path);
            StyleCollection.Instance.Styles.Insert(index, style);
            Directory.Delete(tempDirectoryPath, true);
        }
    }
}
