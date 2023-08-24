using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.WPF.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    public static class MapViewExtension
    {
        public static async Task ZoomToGeometryAsync(this MapView mapView, Geometry geometry, bool autoExtent = true)
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
            await mapView.SetViewpointGeometryAsync(geometry, Config.Instance.HideWatermark && autoExtent ? Config.WatermarkHeight : 0);
        }

    }
}
