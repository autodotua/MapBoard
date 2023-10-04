using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.WPF.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MapBoard.Util.GeometryUtility;

namespace MapBoard.Mapping
{
    public static class MapViewExtension
    {
        /// <summary>
        /// 缩放到指定图形
        /// </summary>
        /// <param name="mapView"></param>
        /// <param name="geometry"></param>
        /// <param name="autoExtent"></param>
        /// <returns></returns>
        public static async Task ZoomToGeometryAsync(this GeoView mapView, Geometry geometry, bool autoExtent = true)
        {
            if (geometry is MapPoint || geometry is Multipoint m && m.Points.Count == 1 || geometry.Extent.Width == 0 || geometry.Extent.Height == 0)
            {
                if (geometry.SpatialReference.Wkid != SpatialReferences.WebMercator.Wkid)
                {
                    geometry = GeometryEngine.Project(geometry, SpatialReferences.WebMercator);
                }
                geometry = GeometryEngine.Buffer(geometry, 500);
            }
            var extent = geometry.Extent;
            if (double.IsNaN(extent.Width) || double.IsNaN(extent.Height) || extent.Width == 0 || extent.Height == 0)
            {
                SnakeBar.ShowError("图形为空");
                return;
            }
            if (mapView is MapView mv)
            {
                await mv.SetViewpointGeometryAsync(geometry, Config.Instance.HideWatermark && autoExtent ? Config.WatermarkHeight : 0);
            }
            else if (mapView is SceneView sv)
            {
                await sv.SetViewpointAsync(new Viewpoint(geometry));
            }
            else
            {
                throw new ArgumentException("必须为MapView类或SceneView类", nameof(mapView));
            }
        }

        /// <summary>
        /// 缩放到记忆的位置
        /// </summary>
        /// <returns></returns>
        public static async Task TryZoomToLastExtent<T>(this T mapView) where T : GeoView, IMapBoardGeoView
        {
            if (mapView.Layers.MapViewExtentJson != null)
            {
                try
                {
                    Envelope envelope = Geometry.FromJson(mapView.Layers.MapViewExtentJson) as Envelope;
                    var point1 = new MapPoint(envelope.XMin, envelope.YMin);
                    var point2 = new MapPoint(envelope.XMax, envelope.YMax);
                    envelope = new Envelope(point1.RegularizeWebMercatorPoint(), point2.RegularizeWebMercatorPoint());
                    await ZoomToGeometryAsync(mapView, envelope, false);
                }
                catch
                {
                }
            }
        }

    }
}
