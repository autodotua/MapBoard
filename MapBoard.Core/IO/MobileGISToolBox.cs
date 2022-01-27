using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MapBoard.IO
{
    public class MobileGISToolBox
    {
        public static async Task ExportLayerAsync(string path, ShapefileMapLayerInfo layer)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "Shapefile");
            Directory.CreateDirectory(tempShpDir);
            await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
            await File.WriteAllTextAsync(Path.Combine(tempShpDir, layer.Name + ".style"), GetStyleJson(layer));
            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir.FullName, path));
        }

        public static async Task ExportMapAsync(string path, MapLayerCollection layers)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "Shapefile");
            Directory.CreateDirectory(tempShpDir);
            foreach (var layer in layers.OfType<ShapefileMapLayerInfo>())
            {
                await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
                await File.WriteAllTextAsync(Path.Combine(tempShpDir, layer.Name + ".style"), GetStyleJson(layer));
            }
            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir.FullName, path));
        }

        private static string GetStyleJson(IMapLayerInfo layer)
        {
            JObject json = new JObject();
            json.Add("Renderer", JObject.Parse(layer.Layer.Renderer.ToJson()));
            JArray jLabels = new JArray();
            foreach (var label in layer.Layer.LabelDefinitions)
            {
                jLabels.Add(JObject.Parse(label.ToJson()));
            }
            json.Add("Labels", jLabels);
            JObject jBasic = new JObject();
            json.Add("Basic", jBasic);
            return json.ToString();
        }
    }
}