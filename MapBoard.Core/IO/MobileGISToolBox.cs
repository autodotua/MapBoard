using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Linq;
using Newtonsoft.Json.Linq;
using MapBoard.Util;
using System.Net;
using System.Threading;
namespace MapBoard.IO
{
    /// <summary>
    /// 与自己写的GIS工具箱的交互
    /// </summary>
    public class MobileGISToolBox
    {
        /// <summary>
        /// 导出一个图层
        /// </summary>
        /// <param name="path"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static async Task ExportLayerAsync(string path, ShapefileMapLayerInfo layer)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "Shapefile");
            Directory.CreateDirectory(tempShpDir);
            await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
            await File.WriteAllTextAsync(Path.Combine(tempShpDir, layer.Name + ".style"), GetStyleJson(layer));
            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir.FullName, path));
        }

        /// <summary>
        /// 导出全部图层
        /// </summary>
        /// <param name="path"></param>
        /// <param name="layers"></param>
        /// <returns></returns>
        public static async Task ExportMapAsync(string path, MapLayerCollection layers)
        {
            string tempDir = await ExportMapToTempDirAsync(layers);
            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir, path));
        }

        /// <summary>
        /// 导出全部图层到临时目录
        /// </summary>
        /// <param name="layers"></param>
        /// <returns></returns>
        public static async Task<string> ExportMapToTempDirAsync(MapLayerCollection layers)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "Shapefile");
            Directory.CreateDirectory(tempShpDir);
            foreach (var layer in layers.OfType<ShapefileMapLayerInfo>())
            {
                await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
                await File.WriteAllTextAsync(Path.Combine(tempShpDir, layer.Name + ".style"), GetStyleJson(layer));
            }
            return tempDir.FullName;
        }
        /// <summary>
        /// 获取能够被GIS工具箱读取的样式文件
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private static string GetStyleJson(IMapLayerInfo layer)
        {
            JObject json = new JObject();
            json.Add(nameof(layer.Layer.Renderer), JObject.Parse(layer.Layer.Renderer.ToJson()));
            JArray jLabels = new JArray();
            foreach (var label in layer.Layer.LabelDefinitions)
            {
                jLabels.Add(JObject.Parse(label.ToJson()));
            }
            json.Add(nameof(layer.Layer.LabelDefinitions), jLabels);
            JObject jDisplay = new JObject
        {
            { nameof(layer.Display.Opacity), layer.Display.Opacity },
            { nameof(layer.Display.MinScale), layer.Display.MinScale },
            { nameof(layer.Display.MaxScale), layer.Display.MaxScale }
        };
            json.Add(nameof(layer.Display), jDisplay);
            return json.ToString();
        }
    }
}