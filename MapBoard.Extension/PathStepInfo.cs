using System;

namespace MapBoard.Extension
{
    public class PathStepInfo
    {
        public Location[] Locations { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// 距离（米）
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// 耗时
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// 收费（元）
        /// </summary>
        public double Tolls { get; set; }

        /// <summary>
        /// 红绿灯个数
        /// </summary>
        public int TrafficLights { get; set; }
    }
}