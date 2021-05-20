using Esri.ArcGISRuntime.Data;
using FzLib.Basic.Collection;
using FzLib.Program;
using FzLib.UI.Dialog;
using MapBoard.Common;

using MapBoard.Common;

using MapBoard.Main.Model;
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
            LayerCollection.Instance.Layers.Clear();
            if (Directory.Exists(Config.DataPath))
            {
                Directory.Delete(Config.DataPath, true);
            }
            ZipFile.ExtractToDirectory(path, Config.DataPath);

            LayerCollection.ResetLayers();
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

            LayerInfo style = Newtonsoft.Json.JsonConvert.DeserializeObject<LayerInfo>(File.ReadAllText(Path.Combine(tempDirectoryPath, "style.json")));
            var files = Shapefile.GetExistShapefiles(tempDirectoryPath, style.Name);

            List<string> copyedFiles = new List<string>();
            foreach (var file in files)
            {
                string target = Path.Combine(Config.DataPath, Path.GetFileName(file));
                if (File.Exists(target))
                {
                    copyedFiles.ForEach(p => File.Delete(p));
                    throw new IOException($"文件{target}已存在");
                }
                File.Move(file, target);
                copyedFiles.Add(target);
            }

            LayerCollection.Instance.Layers.Add(style);
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
            LayerCollection.Instance.Save();
            var Layers = LayerCollection.Instance.Layers.ToArray();
            LayerCollection.Instance.SaveWhenChanged = false;
            LayerCollection.Instance.Layers.Clear();
            Layers.ForEach(p => p.Table = null);

            ZipFile.CreateFromDirectory(Config.DataPath, path);
            Layers.ForEach(p => LayerCollection.Instance.Layers.Add(p));
            LayerCollection.Instance.SaveWhenChanged = true;
        }

        public async static Task ExportMap2Async(string path)
        {
            DirectoryInfo directory = PathUtility.GetTempDir();
            foreach (var layer in LayerCollection.Instance.Layers)
            {
                await Shapefile.CloneFeatureToNewShpAsync(directory.FullName, layer);
            }
            await Task.Delay(500);
            LayerCollection.Instance.Save(Path.Combine(directory.FullName, LayerCollection.LayersFileName));
            ZipFile.CreateFromDirectory(directory.FullName, path);
        }

        /// <summary>
        /// 导出图层包，实则是zip文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="style"></param>
        public static void ExportLayer(string path, LayerInfo style)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            int index = LayerCollection.Instance.Layers.IndexOf(style);
            LayerCollection.Instance.SaveWhenChanged = false;
            LayerCollection.Instance.Layers.Remove(style);
            style.Table = null;

            string tempDirectoryPath = Path.Combine(Config.DataPath, "temp");
            if (Directory.Exists(tempDirectoryPath))
            {
                Directory.Delete(tempDirectoryPath, true);
            }
            Directory.CreateDirectory(tempDirectoryPath);

            foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath, style.Name))
            {
                File.Copy(file, Path.Combine(tempDirectoryPath, Path.GetFileName(file)));
            }
            File.WriteAllText(Path.Combine(tempDirectoryPath, "style.json"), Newtonsoft.Json.JsonConvert.SerializeObject(style));

            ZipFile.CreateFromDirectory(tempDirectoryPath, path);
            LayerCollection.Instance.Layers.Insert(index, style);
            LayerCollection.Instance.SaveWhenChanged = true;
            Directory.Delete(tempDirectoryPath, true);
        }

        public async static Task ExportLayer2Async(string path, LayerInfo layer)
        {
            DirectoryInfo directory = PathUtility.GetTempDir();
            await Shapefile.CloneFeatureToNewShpAsync(directory.FullName, layer);
            await Task.Delay(500);
            File.WriteAllText(Path.Combine(directory.FullName, "style.json"), Newtonsoft.Json.JsonConvert.SerializeObject(layer));

            LayerCollection.Instance.Save(Path.Combine(directory.FullName, LayerCollection.LayersFileName));
            ZipFile.CreateFromDirectory(directory.FullName, path);
        }
    }
}