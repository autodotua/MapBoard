using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.IO.Gpx;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

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

        public void AddPoint(double x, double y, DateTime time, double speed)
        {
            MapPoint thisPoint = new MapPoint(x, y);
            Polyline line = null;
            if (firstPoint == null) //第一个点
            {
                firstPoint = thisPoint;
                return;
            }
            else if (Overlay.Graphics.Count == 0) //第二个点
            {
                line = new Polyline(new MapPoint[] { firstPoint, thisPoint }, SpatialReferences.Wgs84);
            }
            else //第三个及以上的点
            {
                //通过连接最新点、之前的点、之前之前的点共三个点，
                //避免仅连接两个点时会出现的小角度转弯时端点存在“缝隙”的小问题
                var lastLine = Overlay.Graphics[^1].Geometry as Polyline;
                var lastPoint = lastLine.Parts[0].Points[^1];
                var lastLastPoint = lastLine.Parts[0].Points[^2];
                line = new Polyline(new MapPoint[] { lastLastPoint, lastPoint, thisPoint }, SpatialReferences.Wgs84);
            }
            Graphic graphic = new Graphic(line);
            graphic.Attributes.Add("Speed", speed);
            graphic.Attributes.Add("Time", time);
            Overlay.Graphics.Add(graphic);
        }

        public void Clear()
        {
            Overlay.Graphics.Clear();
            firstPoint = null;
        }

        public string GpxFile { get; private set; }

        public async Task<Envelope> LoadColoredGpxAsync(Gpx gpx)
        {
            Clear();
            GpxFile = gpx.FilePath;
            var points = gpx.Tracks[0].Points;
            var speeds = (await GpxSpeedAnalysis.GetSpeedsAsync(points, 3)).ToList();
            var orderedSpeeds = speeds.Select(p => p.Speed).OrderBy(p => p).ToList();
            int speedsCount = speeds.Count;
            int maxIndex = Math.Max(1, (int)(speedsCount * 0.95));//取95%最大值作为速度颜色上线
            int minIndex = Math.Min(speedsCount - 2, (int)(speedsCount * 0.15)); //取15%最小值作为速度颜色下线
            double maxMinusMinSpeed = orderedSpeeds[maxIndex] - orderedSpeeds[minIndex];
            for (int i = 2; i < points.Count; i++)
            {
                MapPoint p1 = new MapPoint(points[i - 2].X, points[i - 2].Y, SpatialReferences.Wgs84);
                MapPoint p2 = new MapPoint(points[i - 1].X, points[i - 1].Y, SpatialReferences.Wgs84);
                MapPoint p3 = new MapPoint(points[i - 0].X, points[i - 0].Y, SpatialReferences.Wgs84);

                //如果两个点距离>300m，速度>1m/s，那么认为信号断连，p1p2之间的连线不显示
                double p1p2Distance = GeometryUtility.GetDistance(p1, p2);
                if (p1p2Distance > 300 && (points[i - 1].Time.Value - points[i - 1].Time.Value).TotalSeconds < 300)
                {
                    //还需要删去上一条线
                    if (Overlay.Graphics.Count > 0)
                    {
                        Overlay.Graphics.RemoveAt(Overlay.Graphics.Count - 1);
                    }
                    Polyline dashLine = new Polyline([p1, p2]);
                    Graphic dashGraphic = new Graphic(dashLine)
                    {
                        Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, Color.LightGray, 6)
                    };
                    Overlay.Graphics.Add(dashGraphic);
                    continue;
                }
                var speed = speeds[i - 2].Speed;
                Polyline line = new Polyline([p1, p2, p3]);
                double speedPercent = Math.Max(0, Math.Min(1, (speed - orderedSpeeds[minIndex]) / maxMinusMinSpeed));
                var color = InterpolateColors([Color.FromArgb(0x54, 0xA5, 0xF6), Color.FromArgb(0xFF, 0xB3, 0x00), Color.FromArgb(0xFF, 0, 0)], speedPercent);
                Graphic graphic = new Graphic(line)
                {
                    Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, 6)
                };
                graphic.Attributes.Add("Speed", speed);
                graphic.Attributes.Add("Time", points[i - 1].Time);
                Overlay.Graphics.Add(graphic);
            }


            //把速度值进行首尾填充，使其数量和点数相同
            //speeds.Insert(0, speeds[0]);
            //speeds.Add(speeds[^1]);
            //for (int i=1;i<points.Count;i++)
            //{
            //    MapPoint p1 = new MapPoint(points[i - 1].X, points[i - 1].Y,SpatialReferences.Wgs84);
            //    MapPoint p2 = new MapPoint(points[i - 0].X, points[i - 0].Y, SpatialReferences.Wgs84);
            //    var speed = 0.5 * (speeds[i].Speed + speeds[i - 1].Speed);
            //    Polyline line = new Polyline(new MapPoint[] { p1, p2 }, SpatialReferences.Wgs84);
            //    double speedPercent = Math.Max(0, Math.Min(1, (speed - orderedSpeeds[minIndex]) / maxMinusMinSpeed));
            //    var color = InterpolateColors([Color.FromArgb(0x54, 0xA5, 0xF6), Color.FromArgb(0xFF, 0xB3, 0x00), Color.FromArgb(0xFF, 0, 0)], speedPercent);
            //    Graphic graphic = new Graphic(line)
            //    {
            //        Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, 6)
            //    };
            //    Overlay.Graphics.Add(graphic);



            //}
            return Overlay.Extent;
        }

        public Polyline LoadLine(IEnumerable<MapPoint> points)
        {
            Clear();
            Polyline line = new Polyline(points, SpatialReferences.Wgs84);
            Overlay.Graphics.Add(new Graphic(line));
            return line;
        }

        private Color InterpolateColors(Color[] colors, double p)
        {
            if (colors.Length < 2 || p <= 0)
            {
                return colors[0];
            }
            else if (p >= 1)
            {
                return colors[^1];
            }
            double segmentLength = 1f / (colors.Length - 1);
            int segmentIndex = (int)(p / segmentLength);
            double segmentProgress = (p - segmentIndex * segmentLength) / segmentLength;

            Color startColor = colors[segmentIndex];
            Color endColor = colors[segmentIndex + 1];

            int r = (int)(startColor.R + (endColor.R - startColor.R) * segmentProgress);
            int g = (int)(startColor.G + (endColor.G - startColor.G) * segmentProgress);
            int b = (int)(startColor.B + (endColor.B - startColor.B) * segmentProgress);

            return Color.FromArgb(r, g, b);
        }
    }
}
