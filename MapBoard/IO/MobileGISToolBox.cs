//using Ionic.Zip;
using MapBoard.Main.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
{
    internal class MobileGISToolBox
    {
        public static async Task ExportLayerAsync(string path, LayerInfo layer)
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

        public static async Task ExportMapAsync(string path)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "BaseShapeFile");
            string tempStyleDir = Path.Combine(tempDir.FullName, "style");
            Directory.CreateDirectory(tempShpDir);
            Directory.CreateDirectory(tempStyleDir);
            foreach (var layer in LayerCollection.Instance)
            {
                await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".uniqueValue.style"), layer.Layer.Renderer.ToJson());
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".label.style"), layer.Layer.LabelDefinitions[0].ToJson());
            }
            ZipFile.CreateFromDirectory(tempDir.FullName, path);
        }
    }
}