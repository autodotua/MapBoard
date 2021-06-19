using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;

namespace MapBoard.IO
{
    public static class Package
    {
        /// <summary>
        /// 导入地图包
        /// </summary>
        /// <param name="path"></param>
        public static async Task ImportMapAsync(string path, MapLayerCollection layers, bool overwrite)
        {
            if (overwrite)
            {
                layers.Clear();
                if (Directory.Exists(Parameters.DataPath))
                {
                    Directory.Delete(Parameters.DataPath, true);
                }
                Directory.CreateDirectory(Parameters.DataPath);
            }
            var tempDir = PathUtility.GetTempDir().FullName;
            ZipFile.ExtractToDirectory(path, tempDir);
            var configPath = Path.Combine(tempDir, MapLayerCollection.LayersFileName);
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("找不到图层配置文件");
            }
            var newLayers = LayerCollection.FromFile(configPath);
            foreach (var layer in newLayers)
            {
                if (layers.Any(p => p.Name == layer.Name))
                {
                    throw new Exception("存在重复的图层名：" + layer.Name);
                }
            }
            foreach (var layer in newLayers)
            {
                foreach (var file in Shapefile.GetExistShapefiles(tempDir, layer.Name))
                {
                    File.Copy(file, Path.Combine(Parameters.DataPath, Path.GetFileName(file)));
                }
                await layers.AddAsync(new MapLayerInfo(layer));
            }
            layers.Save();
        }

        /// <summary>
        /// 导入图层包
        /// </summary>
        /// <param name="path"></param>
        public static async Task ImportLayerAsync(string path, MapLayerCollection layers)
        {
            string tempDirectoryPath = Path.Combine(Parameters.DataPath, "temp");
            if (Directory.Exists(tempDirectoryPath))
            {
                Directory.Delete(tempDirectoryPath, true);
            }
            Directory.CreateDirectory(tempDirectoryPath);

            ZipFile.ExtractToDirectory(path, tempDirectoryPath);

            MapLayerInfo style = Newtonsoft.Json.JsonConvert.DeserializeObject<MapLayerInfo>(File.ReadAllText(Path.Combine(tempDirectoryPath, "style.json")));
            var files = Shapefile.GetExistShapefiles(tempDirectoryPath, style.Name);

            List<string> copyedFiles = new List<string>();
            foreach (var file in files)
            {
                string target = Path.Combine(Parameters.DataPath, Path.GetFileName(file));
                if (File.Exists(target))
                {
                    copyedFiles.ForEach(p => File.Delete(p));
                    throw new IOException($"文件{target}已存在");
                }
                File.Move(file, target);
                copyedFiles.Add(target);
            }

            await layers.AddAsync(style);
        }

        public async static Task ExportMap2Async(string path, MapLayerCollection layers)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            DirectoryInfo directory = PathUtility.GetTempDir();
            foreach (MapLayerInfo layer in layers)
            {
                await Shapefile.CloneFeatureToNewShpAsync(directory.FullName, layer);
            }
            layers.Save(Path.Combine(directory.FullName, MapLayerCollection.LayersFileName));
            ZipFile.CreateFromDirectory(directory.FullName, path);
        }

        public async static Task ExportLayer2Async(string path, MapLayerInfo layer, MapLayerCollection layers)
        {
            DirectoryInfo directory = PathUtility.GetTempDir();
            await Shapefile.CloneFeatureToNewShpAsync(directory.FullName, layer);
            File.WriteAllText(Path.Combine(directory.FullName, "style.json"), Newtonsoft.Json.JsonConvert.SerializeObject(layer));

            layers.Save(Path.Combine(directory.FullName, MapLayerCollection.LayersFileName));
            ZipFile.CreateFromDirectory(directory.FullName, path);
        }

        public async static Task BackupAsync(MapLayerCollection layers, int maxCount)
        {
            await ExportMap2Async(Path.Combine(Parameters.BackupPath, DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mbmpkg"), layers);

            var files = Directory.EnumerateFiles(Parameters.BackupPath).ToList();
            if (files.Count > maxCount)
            {
                foreach (var file in files
                    .Select(p => new FileInfo(p)).ToList()
                    .OrderByDescending(p => p.LastWriteTime).Skip(maxCount).ToList())
                {
                    file.Delete();
                }
            }
        }
    }
}