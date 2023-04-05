using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.UI.Extension
{
    /// <summary>
    /// 网络API扩展面板基类
    /// </summary>
    public abstract class ExtensionPanelBase : UserControlBase
    {
        /// <summary>
        /// 请求用户在地图上点击一个点
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task<MapPoint> GetPointAsync()
        {
            if (MapView is MainMapView m)
            {
                return await m.Editor.GetPointAsync();
            }
            else if (MapView is BrowseSceneView s)
            {
                return await s.GetPointAsync();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="mapView"></param>
        /// <exception cref="NotSupportedException"></exception>
        public void Initialize(GeoView mapView)
        {
            if (mapView is not IMapBoardGeoView)
            {
                throw new NotSupportedException("应实现IMapBoardGeoView接口");
            }
            MapView = mapView;
        }

        /// <summary>
        /// 地图
        /// </summary>
        public GeoView MapView { get; private set; }

        /// <summary>
        /// 覆盖层
        /// </summary>
        public OverlayHelper Overlay => (MapView as IMapBoardGeoView).Overlay;
    }
}