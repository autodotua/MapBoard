using Esri.ArcGISRuntime.Geometry;
using MapBoard;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapBoard.IO.Gpx
{
    /// <summary>
    /// GPX trkseg对象
    /// </summary>
    public class GpxSegment
    {
        public GpxTrack Parent { get; private set; }

        /// <summary>
        /// 轨迹点集
        /// </summary>
        public GpxPointCollection Points { get; private set; } = new GpxPointCollection();

        public double GetDistance()
        {
            double distance = 0;
            MapPoint last = null;
            foreach (var point in Points)
            {
                if (last != null)
                {
                    distance += GeometryUtility.GetDistance(last, point.ToMapPoint());
                }
                last = point.ToMapPoint();
            }

            return distance;
        }

        public TimeSpan? GetTotalTime()
        {
            if (Points.Count<=1)
            {
                return TimeSpan.Zero;
            }
            if (Points[0].Time.HasValue && Points[^1].Time.HasValue)
            {
                return Points[^1].Time.Value - Points[0].Time.Value;
            }
            return null;
        }
    }
    /// <summary>
    /// GPX trk对象
    /// </summary>
    public class GpxTrack : ICloneable
    {
        public double distance = -1;

        private double maxSpeed = -1;

        private TimeSpan? totalTime = null;

        internal GpxTrack(Gpx parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// 平均速度
        /// </summary>
        public double AverageSpeed => Distance / TotalTime.TotalSeconds;

        /// <summary>
        /// 轨迹描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 轨迹名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 其他属性
        /// </summary>
        public Dictionary<string, string> Extensions { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// 对应的GPX对象
        /// </summary>
        internal Gpx Parent { get; set; }


        public object Clone()
        {
            GpxTrack info = MemberwiseClone() as GpxTrack;
            info.Points = Points.Clone() as GpxPointCollection;
            return info;
        }


        /// <summary>
        /// 根据提供的采样率参数，获取最大速度
        /// </summary>
        /// <param name="window"></param>
        /// <param name="jump"></param>
        /// <returns></returns>
        public async Task<double> GetMaxSpeedAsync(int window = 9)
        {
            return (await GpxUtility.GetMeanFilteredSpeedsAsync(Points, window, false)).Max();
        }

        /// <summary>
        /// 获取移动均速
        /// </summary>
        /// <param name="speedDevaluation"></param>
        /// <returns></returns>
        public double GetMovingAverageSpeed(double speedDevaluation = 0.3)
        {
            double totalDistance = 0;
            double totalSeconds = 0;
            GpxPoint last = null;
            try
            {
                foreach (var point in Points.TimeOrderedPoints)
                {
                    if (last != null)
                    {
                        double distance = GeometryUtility.GetDistance(last.ToMapPoint(), point.ToMapPoint());
                        double second = (point.Time - last.Time).Value.TotalSeconds;
                        double speed = distance / second;
                        if (speed > speedDevaluation)
                        {
                            totalDistance += distance;
                            totalSeconds += second;
                        }
                    }
                    last = point;
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("存在没有时间信息的点", ex);
            }
            return totalDistance / totalSeconds;
        }

        /// <summary>
        /// 获取移动时间
        /// </summary>
        /// <param name="speedDevaluation"></param>
        /// <returns></returns>
        public TimeSpan GetMovingTime(double speedDevaluation = 0.3)
        {
            double totalDistance = 0;
            double totalSeconds = 0;
            GpxPoint last = null;
            try
            {
                foreach (var point in Points.TimeOrderedPoints)
                {
                    if (last != null)
                    {
                        double distance = GeometryUtility.GetDistance(last.ToMapPoint(), point.ToMapPoint());
                        double second = (point.Time - last.Time).Value.TotalSeconds;
                        double speed = distance / second;
                        if (speed > speedDevaluation)
                        {
                            totalDistance += distance;
                            totalSeconds += second;
                        }
                    }
                    last = point;
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("存在没有时间信息的点", ex);
            }
            return TimeSpan.FromSeconds(totalSeconds);
        }

    }
}