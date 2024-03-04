using MapBoard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using MapBoard.Model;
using MapBoard.Util;
using Esri.ArcGISRuntime.Geometry;

namespace MapBoard.IO.Gpx
{
    /// <summary>
    /// GPX点集合
    /// </summary>
    public class GpxPointCollection : ObservableCollection<GpxPoint>, ICloneable
    {
        private Envelope extent;

        public GpxPointCollection()
        {
        }

        public GpxPointCollection(IEnumerable<GpxPoint> points)
        {
            foreach (var p in points)
            {
                Add(p);
            }
        }

        /// <summary>
        /// 点的包围盒范围
        /// </summary>
        public Envelope Extent
        {
            get
            {
                if (extent == null)
                {
                    double minX, minY, maxX, maxY;
                    minX = minY = double.MaxValue;
                    maxX = maxY = double.MinValue;
                    foreach (var point in this)
                    {
                        minX = Math.Min(point.X, minX);
                        maxX = Math.Max(point.X, maxX);
                        minY = Math.Min(point.Y, minY);
                        maxY = Math.Max(point.Y, maxY);
                    }

                    return new Envelope(minX, maxY, maxX, minY, SpatialReferences.Wgs84);
                }
                return extent;
            }
        }


        public object Clone()
        {
            GpxPoint[] points = new GpxPoint[Count];
            for (int i = 0; i < Count; i++)
            {
                points[i] = this[i].Clone() as GpxPoint;
            }
            return new GpxPointCollection(points);
        }

        /// <summary>
        /// 获取某一点附近的速度
        /// </summary>
        /// <param name="point">目标点</param>
        /// <param name="unilateralSampleCount">采样点数量，单侧</param>
        /// <returns></returns>
        public double GetSpeed(GpxPoint point, int unilateralSampleCount)
        {
            return GetSpeed(IndexOf(point), unilateralSampleCount);
        }
        /// <summary>
        /// 获取某一点附近的速度
        /// </summary>
        /// <param name="point">目标点在集合中的索引</param>
        /// <param name="unilateralSampleCount">采样点数量，单侧</param>
        /// <returns></returns>
        public double GetSpeed(int index, int unilateralSampleCount)
        {
            if (Count <= 1)
            {
                throw new GpxException("集合拥有的点过少");
            }

            int min = index - unilateralSampleCount;
            if (min < 0)
            {
                min = 0;
            }

            int max = index + unilateralSampleCount;
            if (max > Count - 1)
            {
                max = Count - 1;
            }
            double totalDistance = 0;
            TimeSpan totalTime = TimeSpan.Zero;

            try
            {
                for (int i = min; i < max; i++)
                {
                    totalDistance += GeometryUtility.GetDistance(this[i].ToMapPoint(), this[i + 1].ToMapPoint());
                    totalTime += this[i + 1].Time.Value - this[i].Time.Value;
                }
            }
            catch(InvalidOperationException ex)
            {
                throw new InvalidOperationException("存在没有时间信息的点", ex);
            }
            return totalDistance / totalTime.TotalSeconds;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
        }
    }
}