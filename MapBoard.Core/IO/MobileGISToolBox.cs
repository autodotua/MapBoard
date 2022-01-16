using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Linq;

namespace MapBoard.IO
{
    public class MobileGISToolBox
    {
        public static async Task ExportLayerAsync(string path, ShapefileMapLayerInfo layer)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "BaseShapeFile");
            string tempStyleDir = Path.Combine(tempDir.FullName, "style");
            Directory.CreateDirectory(tempShpDir);
            Directory.CreateDirectory(tempStyleDir);
            await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
            File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".uniqueValue.style"), layer.Layer.Renderer.ToJson());
            if (layer.Layer.LabelDefinitions.Count > 0)
            {
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".label.style"), layer.Layer.LabelDefinitions[0].ToJson());
            }
            ZipFile.CreateFromDirectory(tempDir.FullName, path);
        }

        public static async Task ExportMapAsync(string path, MapLayerCollection layers)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "BaseShapeFile");
            string tempStyleDir = Path.Combine(tempDir.FullName, "style");
            Directory.CreateDirectory(tempShpDir);
            Directory.CreateDirectory(tempStyleDir);
            foreach (var layer in layers.OfType<ShapefileMapLayerInfo>())
            {
                await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".uniqueValue.style"), layer.Layer.Renderer.ToJson());
                if (layer.Layer.LabelDefinitions.Count > 0)
                {
                    File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".label.style"), layer.Layer.LabelDefinitions[0].ToJson());
                }
            }
            ZipFile.CreateFromDirectory(tempDir.FullName, path);
        }
    }
}