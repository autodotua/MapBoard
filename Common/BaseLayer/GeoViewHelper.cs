using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.UI.Dialog;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
                        throw new Exception("加载底图失败", ex);
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
                if (map is SceneView s)
                {
                    if (s.Scene == null)
                    {
                        s.Scene = basemap == null ? new Scene() : new Scene(basemap);
                        await s.Scene.LoadAsync();
                    }
                    else
                    {
                        s.Scene.Basemap = basemap;
                    }
                }
                else if (map is MapView m)
                {
                    if (m.Map == null)
                    {
                        m.Map = basemap == null ? new Map(SpatialReferences.Wgs84) : new Map(basemap);
                        await m.Map.LoadAsync();
                    }
                    else
                    {
                        m.Map.Basemap = basemap;
                    }
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
    }
}