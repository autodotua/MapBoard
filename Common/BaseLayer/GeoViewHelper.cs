﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
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
                        throw new Exception($"加载底图{item.Path}失败", ex);
                    }
                }

                if (basemap.BaseLayers.Count == 0)
                {
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
    }
}