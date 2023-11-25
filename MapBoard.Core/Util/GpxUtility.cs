using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.IO.Gpx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Util
{
    public static class GpxUtility
    {
        /// <summary>
        /// 平滑轨迹
        /// </summary>
        /// <param name="points"></param>
        /// <param name="level"></param>
        /// <param name="get"></param>
        /// <param name="set"></param>
        public static void Smooth(GpxPointCollection points, int level, Func<GpxPoint, double> get, Action<GpxPoint, double> set)
        {
            int count = points.Count;
            Queue<double> queue = new Queue<double>(level);

            for (int headIndex = 0; headIndex < count; headIndex++)
            {
                GpxPoint headPoint = points[headIndex];
                if (queue.Count == level)
                {
                    queue.Dequeue();
                }
                queue.Enqueue(get(headPoint));
                if (headIndex < level)
                {
                    set(points[headIndex / 2], queue.Average());
                }
                else
                {
                    set(points[headIndex - level / 2], queue.Average());
                }
            }
            for (int tailIndex = count - level; tailIndex < count - 1; tailIndex++)
            {
                queue.Dequeue();
                set(points[tailIndex + (count - tailIndex) / 2], queue.Average());
            }
        }

        public static async Task LoadColoredGpxAsync(GpxTrack gpxTrack, GraphicCollection graphics)
        {
            var points = gpxTrack.Points;
            var speeds = (await GpxSpeedAnalysis.GetSpeedsAsync(points, 3)).ToList();
            var orderedSpeeds = speeds.Select(p => p.Speed).OrderBy(p => p).ToList();
            int speedsCount = speeds.Count;
            int maxIndex = 0;
            int minIndex = 0;
            double maxMinusMinSpeed = 0;
            if (speedsCount > 2)
            {
                maxIndex = Math.Max(1, (int)(speedsCount * 0.95));//取95%最大值作为速度颜色上限
                minIndex = Math.Min(speedsCount - 2, (int)(speedsCount * 0.15)); //取15%最小值作为速度颜色下限
                maxMinusMinSpeed = orderedSpeeds[maxIndex] - orderedSpeeds[minIndex];
            }
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
                    if (graphics.Count > 0)
                    {
                        graphics.RemoveAt(graphics.Count - 1);
                    }
                    Polyline dashLine = new Polyline([p1, p2]);
                    Graphic dashGraphic = new Graphic(dashLine)
                    {
                        Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, Color.LightGray, 6)
                    };
                    graphics.Add(dashGraphic);
                    continue;
                }
                var speed = speeds[i - 2].Speed;
                Polyline line = new Polyline([p1, p2, p3]);
                Color color = Color.FromArgb(0x54, 0xA5, 0xF6);
                if (maxMinusMinSpeed > 0)
                {
                    double speedPercent = Math.Max(0, Math.Min(1, (speed - orderedSpeeds[minIndex]) / maxMinusMinSpeed));
                    color = InterpolateColors([Color.FromArgb(0x54, 0xA5, 0xF6), Color.FromArgb(0xFF, 0xB3, 0x00), Color.FromArgb(0xFF, 0, 0)], speedPercent);
                }
                Graphic graphic = new Graphic(line)
                {
                    Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, 6)
                };
                graphic.Attributes.Add("Speed", speed);
                graphic.Attributes.Add("Time", points[i - 1].Time);
                if (points[i - 1].Z.HasValue)
                {
                    graphic.Attributes.Add("Altitude", points[i - 1].Z);
                }
                graphics.Add(graphic);
            }
        }


        private static Color InterpolateColors(Color[] colors, double p)
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