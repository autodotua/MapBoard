using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Control.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Common
{
    public static class MapHelper
    {
        /// <summary>
        /// 为ArcMapView加载底图
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public async static Task<bool> LoadBaseMapsAsync(this MapView map)
        {
            if (!Config.Instance.Url.Contains("{x}") || !Config.Instance.Url.Contains("{y}") || !Config.Instance.Url.Contains("{z}"))
            {
                TaskDialog.ShowError("瓦片地址不包含足够的信息！");
                return false;
            }

            try
            {
                WebTiledLayer baseLayer;

                Basemap basemap = new Basemap();
                foreach (var url in Config.Instance.Urls)
                {
                    baseLayer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
                    basemap.BaseLayers.Add(baseLayer);
                }

                await basemap.LoadAsync();
                if (map.Map != null)
                {
                    map.Map.Basemap = basemap;
                }
                else
                {
                    map.Map = new Map(basemap);
                }

                await map.Map.LoadAsync();
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
