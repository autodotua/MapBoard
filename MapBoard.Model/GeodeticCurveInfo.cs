using System;
using System.Collections.Generic;
using System.Text;

namespace MapBoard.Model
{
    public class GeodeticCurveInfo
    {
        public GeodeticCurveInfo(double distance, Angle azimuth, Angle reverseAzimuth)
        {
            Distance = distance;
            Azimuth = azimuth;
            ReverseAzimuth = reverseAzimuth;
        }

        /// <summary>
        /// 长度
        /// </summary>
        public double Distance { get; }

        /// <summary>
        /// 方位角
        /// </summary>
        public Angle Azimuth { get; }

        /// <summary>
        /// 反方位角
        /// </summary>
        public Angle ReverseAzimuth { get; }
    }
}