using MapBoard.Mapping.Model;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.IO
{
    public class OpenLayers
    {
        public const string JS = @"var map = new ol.Map({
    target: 'map',
    layers: [
        {{base}}
        {{vector}}
    ],
    view: new ol.View({
        center: ol.proj.fromLonLat([121, 30]),
        zoom: 8
    }),
});
map.on('click', function(e) {
    let i=0;
    map.forEachFeatureAtPixel(e.pixel, function (feature, layer) {
       if(i!=0) return;
       i
=1;
       alert(feature.getProperties().description);
    })
}); ";

        private const string JS_Tile = @"new ol.layer.Tile({
            source: new ol.source.XYZ({
                url:
                    '{{url}}',
            }),
        }),";

        private const string JS_KML = @"new ol.layer.Vector({
            source: new ol.source.Vector({
                url: '{{url}}',
                format: new ol.format.KML(),
            }),
        }),";

        public string ExportFolderPath { get; }

        public BaseLayerInfo[] BaseLayers { get; }
        public IMapLayerInfo[] Layers { get; }
        public string[] WebResourcesPath { get; }
        private string mainJS = JS;

        public OpenLayers(string exportFolderPath, string[] webResourcesPath, BaseLayerInfo[] baseLayers, IMapLayerInfo[] layers)
        {
            ExportFolderPath = exportFolderPath;
            WebResourcesPath = webResourcesPath;
            BaseLayers = baseLayers;
            Layers = layers;
        }

        public async Task ExportAsync()
        {
            PrepareFiles();
            InsertBaseLayers();
            await InsertVectorLayersAsync();
            await File.WriteAllTextAsync(Path.Combine(ExportFolderPath, "openlayers.js"), mainJS, Encoding.UTF8);
        }

        private void PrepareFiles()
        {
            if (Directory.Exists(ExportFolderPath))
            {
                Directory.Delete(ExportFolderPath, true);
            }
            Directory.CreateDirectory(ExportFolderPath);

            foreach (var r in WebResourcesPath)
            {
                File.Copy(r, Path.Combine(ExportFolderPath, Path.GetFileName(r)));
            }
        }

        private void InsertBaseLayers()
        {
            StringBuilder str = new StringBuilder();
            foreach (var layer in BaseLayers
                .Where(p => p.Enable && p.Visible)
                .Where(p => p.Type == BaseLayerType.WebTiledLayer))
            {
                str.Append(JS_Tile.Replace("{{url}}", layer.Path));
            }
            mainJS = mainJS.Replace("{{base}}", str.ToString());
        }

        private async Task InsertVectorLayersAsync()
        {
            StringBuilder str = new StringBuilder();
            foreach (var layer in Layers)
            {
                string temp = Path.GetTempFileName() + ".kmz";
                await Kml.ExportAsync(temp, layer);
                using var fs = File.OpenRead(temp);
                using ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read, false);
                zip.Entries[0].ExtractToFile(Path.Combine(ExportFolderPath, $"{layer.Name}.kml"));
                str.Append(JS_KML.Replace("{{url}}", $"{layer.Name}.kml"));
            }
            mainJS = mainJS.Replace("{{vector}}", str.ToString());
        }
    }
}