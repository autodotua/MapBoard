using System;

namespace MapBoard.Extension
{
    public class RouteInfo
    {
        public string Strategy { get; set; }
        public RouteStepInfo[] Steps { get; set; }
        public double Distance { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}