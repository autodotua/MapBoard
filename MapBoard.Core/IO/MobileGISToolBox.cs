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
    public class MobileGISToolBox
    {
        public static async Task OpenHttpServerAsync(string ip, MapLayerCollection layers, CancellationToken cancellationToken)
        {
            var path = Path.GetTempFileName() + ".zip";
            //await ExportMapAsync(path, layers);
            HttpServerUtil server = new HttpServerUtil();
            int port = HttpServerUtil.FreeTcpPort();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            cancellationToken.Register(() => server.Stop());
            await server.Start(ip, port, async req =>
               {
                   if (req.HttpMethod == "GET")
                   {
                       return await File.ReadAllBytesAsync(path);
                   }
                   throw new HttpListenerException(400);
               });
        }

        public static async Task ExportLayerAsync(string path, ShapefileMapLayerInfo layer)
        {
            DirectoryInfo tempDir = PathUtility.GetTempDir();
            string tempShpDir = Path.Combine(tempDir.FullName, "Shapefile");
            Directory.CreateDirectory(tempShpDir);
            await Shapefile.CloneFeatureToNewShpAsync(tempShpDir, layer);
            await File.WriteAllTextAsync(Path.Combine(tempShpDir, layer.Name + ".style"), GetStyleJson(layer));
            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir.FullName, path));
        }

        public static async Task<string> ExportMathToTempDirAsync(MapLayerCollection layers)
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

        public static async Task ExportMapAsync(string path, MapLayerCollection layers)
        {
            string tempDir = await ExportMathToTempDirAsync(layers);
            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir, path));
        }

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
            JObject jDisplay = new JObject();
            jDisplay.Add(nameof(layer.Display.Opacity), layer.Display.Opacity);
            jDisplay.Add(nameof(layer.Display.MinScale), layer.Display.MinScale);
            jDisplay.Add(nameof(layer.Display.MaxScale), layer.Display.MaxScale);
            json.Add(nameof(layer.Display), jDisplay);
            return json.ToString();
        }
    }
}