using Esri.ArcGISRuntime.Geometry;
using FzLib.DataAnalysis;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.IO.Gpx
{
    public class GpxSpeedAnalysis
    {
        public static double GetSpeed(IEnumerable<GpxPoint> points)
        {
            if (points.Any(p => p.Time == null))
            {
                throw new GpxException("其中一个点的时间为空");
            }
            var sortedPoints = points.OrderBy(p => p.Time);
            TimeSpan totalTime = sortedPoints.Last().Time - sortedPoints.First().Time;
            double totalDistance = 0;
            GpxPoint last = null;
            foreach (var point in sortedPoints)
            {
                if (last != null)
                {
                    totalDistance += GeometryUtility.GetDistance(last.ToMapPoint(), point.ToMapPoint());
                }
                last = point;
            }
            return totalDistance / totalTime.TotalSeconds;
        }

        public static double GetSpeed(GpxPoint point1, GpxPoint point2)
        {
            if (point1.Time == null || point2.Time == null)
            {
                throw new GpxException("其中一个点的时间为空");
            }
            return GetSpeed(point1.ToMapPoint(), point2.ToMapPoint(), TimeSpan.FromMilliseconds(Math.Abs((point1.Time - point2.Time).TotalMilliseconds)));
        }

        public static double GetSpeed(MapPoint point1, MapPoint point2, TimeSpan time)
        {
            double distance = GeometryUtility.GetDistance(point1, point2);
            return distance / time.TotalSeconds;
        }

        public async static Task<IEnumerable<SpeedInfo>> GetUsableSpeedsAsync(GpxPointCollection points, int sampleCount = 2)
        {
            return (await GetSpeedsAsync(points, sampleCount))
                .Where(p => !(double.IsNaN(p.Speed) || double.IsInfinity(p.Speed)));
        }

        public async static Task<IReadOnlyList<SpeedInfo>> GetSpeedsAsync(GpxPointCollection points, int sampleCount = 2)
        {
            var speeds = new List<SpeedInfo>();
            await Task.Run(() =>
            {
                Queue<GpxPoint> previousPoints = new Queue<GpxPoint>();
                foreach (var point in points.TimeOrderedPoints)
                {
                    if (previousPoints.Count < sampleCount - 1)
                    {
                        previousPoints.Enqueue(point);
                    }
                    else
                    {
                        previousPoints.Enqueue(point);
                        speeds.Add(new SpeedInfo(previousPoints));
                        previousPoints.Dequeue();
                    }
                }
            });
            return speeds.AsReadOnly();
        }

        /// <summary>
        /// 获取一组点经过滤波后的速度
        /// </summary>
        /// <param name="points">点的集合</param>
        /// <param name="sampleCount">每一组采样点的个数</param>
        /// <param name="jump">每一次循环跳跃的个数。比如设置5，采样10，那么第一轮1-10，第二轮6-15</param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<SpeedInfo>> GetMeanFilteredSpeedsAsync(GpxPointCollection points, int sampleCount, int jump, double min = double.MinValue, double max = double.MaxValue)
        {
            List<SpeedInfo> speedList = new List<SpeedInfo>();
            await Task.Run(() =>
            {
                var sortedPoints = points.TimeOrderedPoints;
                if (sampleCount > sortedPoints.Count)
                {
                    speedList.Add(new SpeedInfo(sortedPoints));
                    return;
                }
                GpxPoint last = null;
                List<double> distances = new List<double>();
                foreach (var point in points)
                {
                    if (last != null)
                    {
                        distances.Add(GeometryUtility.GetDistance(last.ToMapPoint(), point.ToMapPoint()));
                    }
                    last = point;
                }
                for (int i = sampleCount - 1; i < sortedPoints.Count; i += jump)
                {
                    DateTime minTime = sortedPoints[i - sampleCount + 1].Time;
                    DateTime maxTime = sortedPoints[i].Time;
                    double totalDistance = 0;
                    for (int j = i - sampleCount + 1; j < i; j++)
                    {
                        totalDistance += distances[j];
                    }
                    double speed = totalDistance / (maxTime - minTime).TotalSeconds;
                    if (speed < min)
                    {
                        continue;
                    }
                    if (speed > max)
                    {
                        continue;
                    }
                    speedList.Add(new SpeedInfo(minTime, maxTime, speed));
                }
            });
            return speedList.AsReadOnly();
        }

        public async static Task<IReadOnlyList<SpeedInfo>> GetMedianFilteredSpeedsAsync(GpxPointCollection points,
            int sampleCount, int jump, TimeSpan? maxTimeSpan = null,
            double min = double.MinValue, double max = double.MaxValue
           )
        {
            List<SpeedInfo> result = new List<SpeedInfo>();
            await Task.Run(() =>
            {
                var filterResult = Filter.MedianValueFilter(GetSpeedsAsync(points).Result, p => p.Speed, sampleCount, jump);

                //List<SpeedInfo> speeds = new List<SpeedInfo>();
                foreach (var item in filterResult)
                {
                    if (item.SelectedItem.Speed > max || item.SelectedItem.Speed < min)
                    {
                        continue;
                    }
                    var maxTime = item.ReferenceItems.First().CenterTime;
                    var minTime = item.ReferenceItems.Last().CenterTime;
                    if (maxTimeSpan.HasValue && maxTime - minTime > maxTimeSpan)
                    {
                        continue;
                    }
                    SpeedInfo speed = new SpeedInfo(minTime, maxTime, item.SelectedItem.Speed);
                    result.Add(speed);
                }
            });
            return result.AsReadOnly();
        }

        public class SpeedInfo
        {
            public SpeedInfo(DateTime centerTime, double speed)
            {
                CenterTime = centerTime;
                Speed = speed;
            }

            public SpeedInfo(DateTime minTime, DateTime maxTime, double speed)
            {
                TimeSpan = maxTime - minTime;
                CenterTime = minTime + TimeSpan.FromMilliseconds(TimeSpan.TotalMilliseconds / 2);
                Speed = speed;
            }

            public SpeedInfo(IEnumerable<GpxPoint> points) : this(points.ToArray())
            {
            }

            public SpeedInfo(params GpxPoint[] points)
            {
                List<GpxPoint> relatedPointList = new List<GpxPoint>();
                DateTime minTime = DateTime.MaxValue;
                DateTime maxTime = DateTime.MinValue;
                foreach (var point in points)
                {
                    if (point.Time < minTime)
                    {
                        minTime = point.Time;
                    }
                    if (point.Time > maxTime)
                    {
                        maxTime = point.Time;
                    }
                    relatedPointList.Add(point);
                }
                if (relatedPointList.Count < 2)
                {
                    throw new GpxException("点数量过少");
                }
                RelatedPoints = relatedPointList.ToArray();
                TimeSpan = maxTime - minTime;
                CenterTime = minTime + TimeSpan.FromMilliseconds(TimeSpan.TotalMilliseconds / 2);
                if (relatedPointList.Count == 2)
                {
                    Speed = GetSpeed(RelatedPoints[0], RelatedPoints[1]);
                }
                else
                {
                    Speed = GetSpeed(RelatedPoints);
                }
            }

            public GpxPoint[] RelatedPoints { get; private set; }
            public TimeSpan TimeSpan { get; private set; }
            public DateTime CenterTime { get; private set; }
            public double Speed { get; private set; }
        }
    }
}