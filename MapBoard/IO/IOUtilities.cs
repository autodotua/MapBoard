using Esri.ArcGISRuntime.Data;
using MapBoard.Common.Resource;
using MapBoard.Main.Layer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
{
    public static class IOUtilities
    {
        public static DirectoryInfo GetTempDir()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            DirectoryInfo directory = new DirectoryInfo(tempDirectory);
            if (directory.Exists)
            {
                directory.Delete(true);
            }
            directory.Create();
            return directory;
        }

        public static async Task<string> CloneFeatureToNewShp(string directory, LayerInfo layer)
        {
            string path = ShapefileExport.ExportEmptyShapefile(layer.Type, layer.Name, directory);
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.AddFeaturesAsync(await layer.GetAllFeatures());
            table.Close();
            return path;
        }
    }
}