using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    public class TrackOverlayHelper
    {
        MapPoint firstPoint = null;

        public TrackOverlayHelper(GraphicsOverlay overlay)
        {
            Overlay = overlay;
        }

        public GraphicsOverlay Overlay { get; }

        public void AddPoint(double x, double y)
        {
            MapPoint thisPoint = new MapPoint(x, y);
            if (firstPoint == null) //第一个点
            {
                firstPoint = thisPoint;
            }
            else if (Overlay.Graphics.Count == 0) //第二个点
            {
                Polyline line = new Polyline(new MapPoint[] { firstPoint, thisPoint }, SpatialReferences.Wgs84);
                Overlay.Graphics.Add(new Graphic(line));
            }
            else //第三个及以上的点
            {
                //通过连接最新点、之前的点、之前之前的点共三个点，
                //避免仅连接两个点时会出现的小角度转弯时端点存在“缝隙”的小问题
                var lastLine = Overlay.Graphics[^1].Geometry as Polyline;
                var lastPoint = lastLine.Parts[0].Points[^1];
                var lastLastPoint = lastLine.Parts[0].Points[^2];
                Polyline line = new Polyline(new MapPoint[] { lastLastPoint, lastPoint, thisPoint }, SpatialReferences.Wgs84);
                Overlay.Graphics.Add(new Graphic(line));
            }
        }

        public void Clear()
        {
            Overlay.Graphics.Clear();
            firstPoint = null;
        }

        public Polyline LoadLine(IEnumerable<MapPoint> points)
        {
            Clear();
            Polyline line = new Polyline(points, SpatialReferences.Wgs84);
            Overlay.Graphics.Add(new Graphic(line));
            return line;
        }
    }
}
