using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.UI.Dialog;
using ModernWpf.FzExtension.CommonDialog;
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
                        await CommonDialog.ShowErrorDialogAsync(ex, "加载底图失败");
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
                await CommonDialog.ShowErrorDialogAsync(ex, "加载底图失败");

                return false;
            }
        }
    }
}