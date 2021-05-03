using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.UI.Dialog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Common.BaseLayer
{
    public static class GeoViewHelper
    {
        /// <summary>
        /// 为ArcSceneView或ArcMapView加载底图
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public async static Task<bool> LoadBaseGeoViewAsync(GeoView map)
        {
            try
            {
                Basemap basemap = new Basemap();

                foreach (var item in Config.Instance.BaseLayers.Where(p => p.Enable))
                {
                    try
                    {
                        BaseLayerHelper.AddLayer(basemap, item);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.ShowException(ex, $"加载以下底图失败：{Environment.NewLine}");
                    }
                }

                if (basemap.BaseLayers.Count == 0)
                {
                    basemap = null;
                }
                else
                {
                    await basemap.LoadAsync();
                }
                if (map is SceneView)
                {
                    (map as SceneView).Scene = basemap == null ? new Scene() : new Scene(basemap);
                    await (map as SceneView).Scene.LoadAsync();
                }
                else
                {
                    (map as MapView).Map = basemap == null ? new Map(SpatialReferences.Wgs84) : new Map(basemap);
                    await (map as MapView).Map.LoadAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                TaskDialog.ShowException(ex, "加载地图失败");

                return false;
            }
        }
    }
}