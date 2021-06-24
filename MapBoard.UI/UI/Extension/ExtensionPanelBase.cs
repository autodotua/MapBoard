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
    public abstract class ExtensionPanelBase : UserControlBase
    {
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

        public void Initialize(GeoView mapView)
        {
            if (mapView is not IMapBoardGeoView)
            {
                throw new NotSupportedException("应实现IMapBoardGeoView接口");
            }
            MapView = mapView;
        }

        public GeoView MapView { get; private set; }
        public OverlayHelper Overlay => (MapView as IMapBoardGeoView).Overlay;
    }
}