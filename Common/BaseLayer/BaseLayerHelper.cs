using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Common.BaseLayer
{
    public static class BaseLayerHelper
    {
        public const string WebTiledLayer = nameof(WebTiledLayer);
        public const string RasterLayer = nameof(RasterLayer);
        public const string ShapefileLayer = nameof(ShapefileLayer);
        public const string TpkLayer = nameof(TpkLayer);


        public static Layer AddLayer(Basemap map,BaseLayerInfo baseLayer)
        {
            var type = baseLayer.Type;
            var arg = baseLayer.Path;
            Layer layer= type switch
            {
             BaseLayerType.WebTiledLayer => AddTiledLayer(map, arg),
             BaseLayerType.RasterLayer => AddRasterLayer(map, arg),
             BaseLayerType.ShapefileLayer => AddShapefileLayer(map, arg),
             BaseLayerType.TpkLayer => AddTpkLayer(map, arg),
                _ => throw new Exception("未知类型"),
            };
            layer.Opacity = baseLayer.Opacity;
            return layer;
        }

        private static WebTiledLayer AddTiledLayer(Basemap map, string url)
        {
            WebTiledLayer layer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
            map.BaseLayers.Add(layer);
            return layer;
        }

        private static RasterLayer AddRasterLayer(Basemap map, string path)
        {
            RasterLayer layer = new RasterLayer(path);
            map.BaseLayers.Add(layer);
            return layer;
        }     

        private static ArcGISTiledLayer AddTpkLayer(Basemap map, string path)
        {
            TileCache cache = new TileCache(path);
            ArcGISTiledLayer layer = new ArcGISTiledLayer(cache);
            map.BaseLayers.Add(layer);
            return layer;
        }     
        
        private static FeatureLayer AddShapefileLayer(Basemap map, string path)
        {
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            FeatureLayer layer = new FeatureLayer(table);
            map.BaseLayers.Add(layer);
            return layer;
        }

        public static void SetWebTiledLayerRequest(WebTiledLayer layer)
        {
    
        }

    }
}
