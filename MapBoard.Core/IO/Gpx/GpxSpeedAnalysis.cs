﻿using Esri.ArcGISRuntime.Geometry;
using FzLib.DataAnalysis;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.IO.Gpx
{
    /// <summary>
    /// GPX速度分析和数据处理
    /// </summary>
    public class GpxSpeedAnalysis
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
                            Debug.WriteLine($"left={left}, right={right}, distance={distance}, speed={speed}, index={index}");
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

        /// <summary>
        /// 获取一组点经过均值滤波后的速度
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
                try
                {
                    for (int i = sampleCount - 1; i < sortedPoints.Count; i += jump)
                    {
                        DateTime minTime = sortedPoints[i - sampleCount + 1].Time.Value;
                        DateTime maxTime = sortedPoints[i].Time.Value;
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
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("存在没有时间信息的点", ex);
                }
            });
            return speedList.AsReadOnly();
        }

        /// <summary>
        /// 获取一组点经过中值滤波后的速度
        /// </summary>
        /// <param name="points"></param>
        /// <param name="sampleCount"></param>
        /// <param name="jump"></param>
        /// <param name="maxTimeSpan"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 根据点集的总路程和总时间计算速度
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static double GetSpeed(IEnumerable<GpxPoint> points)
        {
            var sortedPoints = points.OrderBy(p => p.Time);
            TimeSpan? totalTime = sortedPoints.Last().Time - sortedPoints.First().Time;
            if (!totalTime.HasValue)
            {
                throw new InvalidOperationException("存在没有时间信息的点");
            }
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
            return totalDistance / totalTime.Value.TotalSeconds;
        }

        /// <summary>
        /// 根据带时间信息的两个点计算速度
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static double GetSpeed(GpxPoint point1, GpxPoint point2)
        {
            return GetSpeed(point1.ToMapPoint(), point2.ToMapPoint(),
                TimeSpan.FromMilliseconds(Math.Abs((point1.Time - point2.Time)?.TotalMilliseconds
                ?? throw new InvalidOperationException("存在没有时间信息的点"))));
        }

        /// <summary>
        /// 根据两点和时间差计算速度
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static double GetSpeed(MapPoint point1, MapPoint point2, TimeSpan time)
        {
            double distance = GeometryUtility.GetDistance(point1, point2);
            return distance / time.TotalSeconds;
        }

        /// <summary>
        /// 计算点集中每个点的速度
        /// </summary>
        /// <param name="points"></param>
        /// <param name="sampleCount">采样数</param>
        /// <returns></returns>
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
        /// 计算点集中每个点的速度，只提供有效值
        /// </summary>
        /// <param name="points"></param>
        /// <param name="sampleCount">采样数</param>
        /// <returns></returns>
        public async static Task<IEnumerable<SpeedInfo>> GetUsableSpeedsAsync(GpxPointCollection points, int sampleCount = 2)
        {
            return (await GetSpeedsAsync(points, sampleCount))
                .Where(p => !(double.IsNaN(p.Speed) || double.IsInfinity(p.Speed)));
        }

        /// <summary>
        /// 速度信息
        /// </summary>
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
                try
                {
                    foreach (var point in points)
                    {
                        if (point.Time < minTime)
                        {
                            minTime = point.Time.Value;
                        }
                        if (point.Time > maxTime)
                        {
                            maxTime = point.Time.Value;
                        }
                        relatedPointList.Add(point);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("存在没有时间信息的点", ex);
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

            /// <summary>
            /// 中央时间
            /// </summary>
            public DateTime CenterTime { get; private set; }

            /// <summary>
            /// 相关点
            /// </summary>
            public GpxPoint[] RelatedPoints { get; private set; }

            /// <summary>
            /// 平均速度
            /// </summary>
            public double Speed { get; private set; }

            /// <summary>
            /// 采样总时长
            /// </summary>
            public TimeSpan TimeSpan { get; private set; }
        }
    }
}