//using Ionic.Zip;
using MapBoard.Main.Layer;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
{
    class MobileGISToolBox
    {
        public static async Task ExportLayer(string path, LayerInfo layer)
        {
            //ZipFile zip;
            //using (zip = new ZipFile(path))
            //{
            //zip.AlternateEncoding = Encoding.UTF8;
            //zip.AlternateEncodingUsage = ZipOption.Always;

            DirectoryInfo tempDir = IOUtilities.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "BaseShapeFile");
            string tempStyleDir = Path.Combine(tempDir.FullName, "style");
            Directory.CreateDirectory(tempShpDir);
            Directory.CreateDirectory(tempStyleDir);
            await IOUtilities.CloneFeatureToNewShp(tempShpDir, layer);
            File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".uniqueValue.style"), layer.Layer.Renderer.ToJson());
            File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".label.style"), layer.Layer.LabelDefinitions[0].ToJson());
            ZipFile.CreateFromDirectory(tempDir.FullName, path);
            //foreach (var file in tempShpDir.EnumerateFiles())
            //{
            //    zip.AddFile( file.FullName, "BaseShapeFile");
            //}

            //zip.Save();
            //}
        }
        public static async Task ExportMap(string path)
        {
            //ZipFile zip;
            //using (zip = new ZipFile(path))
            //{
            //zip.AlternateEncoding = Encoding.UTF8;
            //zip.AlternateEncodingUsage = ZipOption.Always;

            DirectoryInfo tempDir = IOUtilities.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "BaseShapeFile");
            string tempStyleDir = Path.Combine(tempDir.FullName, "style");
            Directory.CreateDirectory(tempShpDir);
            Directory.CreateDirectory(tempStyleDir);
            foreach (var layer in LayerCollection.Instance.Layers)
            {
                await IOUtilities.CloneFeatureToNewShp(tempShpDir, layer);
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".uniqueValue.style"), layer.Layer.Renderer.ToJson());
                File.WriteAllText(Path.Combine(tempStyleDir, layer.Name + ".label.style"), layer.Layer.LabelDefinitions[0].ToJson());

            }
            ZipFile.CreateFromDirectory(tempDir.FullName, path);
            //foreach (var file in tempShpDir.EnumerateFiles())
            //{
            //    zip.AddFile( file.FullName, "BaseShapeFile");
            //}

            //zip.Save();
            //}
        }
    }
}
