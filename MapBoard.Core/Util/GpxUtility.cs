using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.IO.Gpx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Util
{
    public static class GpxUtility
    {
        /// <summary>
        /// 获取一组点经过均值滤波后的速度
        /// </summary>
        /// <param name="points">点的集合</param>
        /// <param name="window">每一组采样点的个数</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<double>> GetMeanFilteredSpeedsAsync(GpxPointCollection points, int window, bool writeToPoints)
        {
            if (window % 2 == 0 || window < 3)
            {
                throw new ArgumentException("窗口大小必须为大于等于3的奇数", nameof(window));
            }
            double[] speeds = new double[points.Count];
            double[] distances = new double[points.Count - 1];
            points = points.TimeOrderedPoints;
            await Task.Run(() =>
            {
                try
                {
                    int left = 0;//左端索引
                    int right = 2;//右端索引（包含，因此长度为right-left+1）
                    MapPoint lastPoint = points[1].ToMapPoint();
                    double distance = GeometryUtility.GetDistance(points[0].ToMapPoint(), points[1].ToMapPoint());
                    distances[0] = distance;
                    while (right == 0 || left < right)
                    {
                        if ((right - left) % 2 == 0) //在左端或右端时，窗口大小可能为偶数，此时不进行处理
                        {
                            var currentPoint = points[right].ToMapPoint();
                            distances[right - 1] = GeometryUtility.GetDistance(lastPoint, currentPoint);
                            lastPoint = currentPoint;
                            distance += distances[right - 1];
                            var speed = distance / ((points[right].Time.Value.Ticks - points[left].Time.Value.Ticks) / 10_000_000.0);
                            int index = (right + left) / 2;
                            speeds[index] = speed;
                            //Debug.WriteLine($"left={left}, right={right}, distance={distance}, speed={speed}, index={index}");
                            if (writeToPoints)
                            {
                                points[index].Speed = speed;
                            }
                        }
                        if (right == points.Count - 1)//右端已经到底
                        {
                            distance -= distances[left];
                            left++;
                        }
                        else if (right - left + 1 == window)//到达窗口大小
                        {
                            distance -= distances[left];
                            left++;
                            right++;
                        }
                        else
                        {
                            Debug.Assert(left == 0);
                            right++;
                        }
                    }
                    speeds[0] = speeds[1];
                    speeds[^1] = speeds[^2];
                    if (writeToPoints)
                    {
                        points[0].Speed = speeds[0];
                        points[^1].Speed = speeds[^1];
                    }


                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("存在没有时间信息的点", ex);
                }
            });
            return speeds.AsReadOnly();
        }

        public static async Task LoadColoredGpxAsync(GpxTrack gpxTrack, GraphicCollection graphics)
        {
            var points = gpxTrack.Points;
            var speeds = await GetMeanFilteredSpeedsAsync(points, Parameters.GpxSpeedSmoothWindow, false);
            var orderedSpeeds = speeds.OrderBy(p => p).ToList();
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
            double distance = 0;
            for (int i = 2; i < points.Count; i++)
            {
                MapPoint p1 = new MapPoint(points[i - 2].X, points[i - 2].Y, points[i - 2].Z ?? 0, SpatialReferences.Wgs84);
                MapPoint p2 = new MapPoint(points[i - 1].X, points[i - 1].Y, points[i - 1].Z ?? 0, SpatialReferences.Wgs84);
                MapPoint p3 = new MapPoint(points[i - 0].X, points[i - 0].Y, points[i - 0].Z ?? 0, SpatialReferences.Wgs84);

                if(distance==0)
                {
                    distance = GeometryUtility.GetDistance(p1, p2);
                }
                distance += GeometryUtility.GetDistance(p2, p3);
                //如果两个点时间差超过5分钟，那么认为信号断连，p1p2之间的连线不显示
                if ((points[i - 1].Time.Value - points[i - 2].Time.Value).TotalMinutes > 5)
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
                var speed = speeds[i - 2];
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
                graphic.Attributes.Add("Distance", distance);
                graphics.Add(graphic);
            }
        }

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