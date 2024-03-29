﻿using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Threading;

namespace MapBoard.IO
{
    /// <summary>
    /// 基于Zip的地图画板工程文件包
    /// </summary>
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
                FolderPaths.SwitchToNewDataPath();
            }
            var tempDir = PathUtility.GetTempDir().FullName;
            await Task.Run(() => ZipFile.ExtractToDirectory(path, tempDir));
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
                    throw new ArgumentException("存在重复的图层名：" + layer.Name);
                }
                if (!MapLayerInfo.Types.GetSupportedTypeNames().Contains(layer.Type))
                {
                    throw new NotSupportedException("不支持的图层类型：" + layer.Type);
                }
            }
            foreach (var layer in newLayers)
            {
                switch (layer.Type)
                {
                    case null:
                    case "":
                    case MapLayerInfo.Types.Shapefile:
                        await Task.Run(() =>
                        {
                            foreach (var file in Shapefile.GetExistShapefiles(tempDir, layer.Name))
                            {
                                File.Copy(file, Path.Combine(FolderPaths.DataPath, Path.GetFileName(file)));
                            }
                        });
                        break;
                }

                await layers.AddAsync(layer);
            }
            layers.Save();
        }

        /// <summary>
        /// 导入图层包
        /// </summary>
        /// <param name="path"></param>
        public static async Task ImportLayerAsync(string path, MapLayerCollection layers)
        {
            string tempDirectoryPath = Path.Combine(FolderPaths.DataPath, "temp");
            if (Directory.Exists(tempDirectoryPath))
            {
                Directory.Delete(tempDirectoryPath, true);
            }
            Directory.CreateDirectory(tempDirectoryPath);

            ZipFile.ExtractToDirectory(path, tempDirectoryPath);

            LayerInfo layer = Newtonsoft.Json.JsonConvert.DeserializeObject<LayerInfo>(
                   File.ReadAllText(Path.Combine(tempDirectoryPath, "style.json")));

            switch (layer.Type)
            {
                case null:
                case "":
                case MapLayerInfo.Types.Shapefile:
                    foreach (var file in Shapefile.GetExistShapefiles(tempDirectoryPath, layer.Name))
                    {
                        File.Copy(file, Path.Combine(FolderPaths.DataPath, Path.GetFileName(file)));
                    }
                    break;
            }
            await layers.AddAsync(layer);
        }

        /// <summary>
        /// 导出所有图层到地图包
        /// </summary>
        /// <param name="path">目标路径</param>
        /// <param name="layers">图层集合</param>
        /// <param name="copyOnly">是否仅简单复制而非重建</param>
        /// <returns></returns>
        public static async Task ExportMapAsync(string path, MapLayerCollection layers, bool copyOnly)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            DirectoryInfo directory = PathUtility.GetTempDir();
            if (copyOnly)
            {
                await Task.Run(() =>
                 {
                     foreach (var layer in layers.OfType<IFileBasedLayer>())
                     {
                         CopyLayerFiles(directory.FullName, layer);
                     }
                 });
            }
            else
            {
                //await Parallel.ForEachAsync(layers.OfType<IFileBasedLayer>(), async (layer, cancel) =>
                // {
                //     await layer.SaveTo(directory.FullName);
                // });

                foreach (IFileBasedLayer layer in layers)
                {
                    await layer.SaveTo(directory.FullName);
                }
            }
            layers.Save(Path.Combine(directory.FullName, MapLayerCollection.LayersFileName));

            await ZipDirAsync(directory.FullName, path);
        }

        /// <summary>
        /// 导出图层到图层包
        /// </summary>
        /// <param name="path"></param>
        /// <param name="layer"></param>
        /// <param name="layers">图层集合</param>
        /// <param name="copyOnly">是否仅简单复制而非重建</param>
        /// <returns></returns>
        public static async Task ExportLayerAsync(string path, IMapLayerInfo layer, bool copyOnly)
        {
            DirectoryInfo directory = PathUtility.GetTempDir();
            if (layer is IFileBasedLayer f)
            {
                if (copyOnly)
                {
                    await Task.Run(() =>
                    {
                        CopyLayerFiles(directory.FullName, f);
                    });
                }
                else
                {
                    await f.SaveTo(directory.FullName);
                }
            }

            File.WriteAllText(Path.Combine(directory.FullName, "style.json"), Newtonsoft.Json.JsonConvert.SerializeObject(layer));

            await ZipDirAsync(directory.FullName, path);
        }

        /// <summary>
        /// 压缩一个目录
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static async Task ZipDirAsync(string dir, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(dir, path);
            });
        }

        /// <summary>
        /// 备份
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="maxCount"></param>
        /// <param name="copyOnly"></param>
        /// <returns></returns>
        public static async Task BackupAsync(MapLayerCollection layers, int maxCount, bool copyOnly)
        {
            await ExportMapAsync(Path.Combine(FolderPaths.BackupPath,
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mbmpkg"),
                layers, copyOnly);

            var files = Directory.EnumerateFiles(FolderPaths.BackupPath).ToList();
            if (files.Count > maxCount)
            {
                foreach (var file in files
                    .Select(p => new FileInfo(p))
                    .ToList()
                    .OrderByDescending(p => p.LastWriteTime)
                    .Skip(maxCount)
                    .ToList())
                {
                    file.Delete();
                }
            }
        }

        /// <summary>
        /// 复制图层文件
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="layer"></param>
        private static void CopyLayerFiles(string directory, IFileBasedLayer layer)
        {
            var files = layer.GetFilePaths();
            foreach (var file in files)
            {
                File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
            }
        }
    }
}