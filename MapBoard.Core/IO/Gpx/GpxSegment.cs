using System;
using System.Collections.Generic;

namespace MapBoard.IO.Gpx
{
    /// <summary>
    /// GPX trkseg对象
    /// </summary>
    public class GpxSegment : ICloneable
    {
        internal GpxSegment(GpxTrack parent)
        {
            Parent = parent;
        }
        public GpxTrack Parent { get; private set; }

        /// <summary>
        /// 轨迹点集
        /// </summary>
        public IList<GpxPoint> Points { get; private set; } = new List<GpxPoint>();

        public object Clone()
        {
            var newObj = MemberwiseClone() as GpxSegment;
            newObj.Points = new List<GpxPoint>(Points);
            return newObj;
        }
    }
}