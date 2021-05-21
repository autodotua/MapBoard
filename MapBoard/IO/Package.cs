﻿using Esri.ArcGISRuntime.Data;
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
        public static async Task ImportMapAsync(string path, bool overwrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            if (overwrite)
            {
                LayerCollection.Instance.Clear();
                if (Directory.Exists(Config.DataPath))
                {
                    Directory.Delete(Config.DataPath, true);
                }

                ZipFile.ExtractToDirectory(path, Config.DataPath);
                await LayerCollection.ResetLayersAsync();
            }
            else
            {
                var tempDir = PathUtility.GetTempDir().FullName;
                ZipFile.ExtractToDirectory(path, tempDir);
                var configPath = Path.Combine(tempDir, LayerCollection.LayersFileName);
                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException("找不到图层配置文件");
                }
                var layers = LayerCollection.GetInstance(configPath);
                foreach (var layer in layers)
                {
                    if (LayerCollection.Instance.Any(p => p.Name == layer.Name))
                    {
                        throw new Exception("存在重复的图层名：" + layer.Name);
                    }
                }
                foreach (var layer in layers)
                {
                    foreach (var file in Shapefile.GetExistShapefiles(tempDir, layer.Name))
                    {
                        File.Copy(file, Path.Combine(Config.DataPath, Path.GetFileName(file)));
                    }
                    LayerCollection.Instance.AddAsync(layer);
                }
            }
        }

        /// <summary>
        /// 导入图层包
        /// </summary>
        /// <param name="path"></param>
        public static async Task ImportLayerAsync(string path)
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

            await LayerCollection.Instance.AddAsync(style);
        }

        public async static Task ExportMap2Async(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            DirectoryInfo directory = PathUtility.GetTempDir();
            foreach (var layer in LayerCollection.Instance)
            {
                await Shapefile.CloneFeatureToNewShpAsync(directory.FullName, layer);
            }
            LayerCollection.Instance.Save(Path.Combine(directory.FullName, LayerCollection.LayersFileName));
            ZipFile.CreateFromDirectory(directory.FullName, path);
        }

        public async static Task ExportLayer2Async(string path, LayerInfo layer)
        {
            DirectoryInfo directory = PathUtility.GetTempDir();
            await Shapefile.CloneFeatureToNewShpAsync(directory.FullName, layer);
            File.WriteAllText(Path.Combine(directory.FullName, "style.json"), Newtonsoft.Json.JsonConvert.SerializeObject(layer));

            LayerCollection.Instance.Save(Path.Combine(directory.FullName, LayerCollection.LayersFileName));
            ZipFile.CreateFromDirectory(directory.FullName, path);
        }

        public async static Task BackupAsync(int maxCount)
        {
            await ExportMap2Async(Path.Combine(Config.BackupPath, DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mbmpkg"));

            var files = Directory.EnumerateFiles(Config.BackupPath).ToList();
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