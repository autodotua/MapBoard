//using Ionic.Zip;
using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;

namespace MapBoard.IO
{
    public class MobileGISToolBox
    {
        public static async Task ExportLayerAsync(string path, MapLayerInfo layer)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "BaseShapeFile");
            string tempStyleDir = Path.Combine(tempDir.FullName, "style");
            Directory.CreateDirectory(tempShpDir);
            Directory.CreateDirectory(tempStyleDir);
            await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
            File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".uniqueValue.style"), layer.Layer.Renderer.ToJson());
            File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".label.style"), layer.Layer.LabelDefinitions[0].ToJson());
            ZipFile.CreateFromDirectory(tempDir.FullName, path);
        }

        public static async Task ExportMapAsync(string path, MapLayerCollection layers)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "BaseShapeFile");
            string tempStyleDir = Path.Combine(tempDir.FullName, "style");
            Directory.CreateDirectory(tempShpDir);
            Directory.CreateDirectory(tempStyleDir);
            foreach (MapLayerInfo layer in layers)
            {
                await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".uniqueValue.style"), layer.Layer.Renderer.ToJson());
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".label.style"), layer.Layer.LabelDefinitions[0].ToJson());
            }
            ZipFile.CreateFromDirectory(tempDir.FullName, path);
        }
    }
}