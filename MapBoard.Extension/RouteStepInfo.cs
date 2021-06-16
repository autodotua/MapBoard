using System;

namespace MapBoard.Extension
{
    public class RouteStepInfo
    {
        public Location[] Locations { get; set; }

        /// <summary>
        /// 路名
        /// </summary>
        public string Road { get; set; }

        /// <summary>
        /// 距离（米）
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// 耗时
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }
}