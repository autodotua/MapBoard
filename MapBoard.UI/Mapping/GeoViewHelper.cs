using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using MapBoard.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace MapBoard.Mapping
{
    public static class GeoViewHelper
    {
        /// <summary>
        /// 为ArcSceneView或ArcMapView加载底图
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public async static Task<bool> LoadBaseGeoViewAsync(this GeoView map)
        {
            try
            {
                Basemap basemap = new Basemap();

                foreach (var item in Config.Instance.BaseLayers.Reverse<BaseLayerInfo>())
                {
                    try
                    {
                        var layer = AddLayer(basemap, item);
                        layer.Id = item.TempID.ToString();
                        layer.IsVisible = item.Enable;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"加载底图{item.Path}失败", ex);
                    }
                }

                if (basemap.BaseLayers.Count == 0)
                {
                    basemap = new Basemap(new RasterLayer(Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "res", "DefaultBaseMap.jpg")));
                }
                await basemap.LoadAsync();
                if (map is SceneView s)
                {
                    if (s.Scene == null)
                    {
                        s.Scene = basemap.BaseLayers.Count == 0 ? new Scene() : new Scene(basemap);
                    }
                    else
                    {
                        s.Scene.Basemap = basemap;
                    }
                    await s.Scene.LoadAsync();
                }
                else if (map is MapView m)
                {
                    if (m.Map == null)
                    {
                        m.Map = basemap.BaseLayers.Count == 0 ? new Map(SpatialReferences.Wgs84) : new Map(basemap);
                    }
                    else
                    {
                        m.Map.Basemap = basemap;
                    }
                    await m.Map.LoadAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("加载底图失败", ex);
            }
        }

        public static void SetHideWatermark(this GeoView map)
        {
            map.Margin = new Thickness(Config.Instance.HideWatermark ? -Config.WatermarkHeight : 0);
        }

        public const string WebTiledLayer = nameof(WebTiledLayer);
        public const string RasterLayer = nameof(RasterLayer);
        public const string ShapefileLayer = nameof(ShapefileLayer);
        public const string TpkLayer = nameof(TpkLayer);

        public static Layer AddLayer(Basemap map, BaseLayerInfo baseLayer)
        {
            var type = baseLayer.Type;
            var arg = baseLayer.Path;
            Layer layer = type switch
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
    }
}