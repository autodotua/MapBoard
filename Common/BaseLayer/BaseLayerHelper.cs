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
        public const string WebTiledLayerDescription = "网络切片";
        public const string RasterLayerDescription = "位图";
        public const string ShapefileLayerDescription = "Shp矢量图层";
        public const string TpkLayerDescription = "切片包";

        public static Layer AddLayer(Basemap map,string type,string arg)
        {
            switch(type)
            {
                case WebTiledLayer:
                  return  AddTiledLayer(map, arg);
                case RasterLayer:
                    return AddRasterLayer(map, arg);
                case ShapefileLayer:
                    return AddShapefileLayer(map, arg);
                case TpkLayer:
                    return AddTpkLayer(map, arg);
                default:
                    throw new Exception("未知类型");
            }
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



    }
}
