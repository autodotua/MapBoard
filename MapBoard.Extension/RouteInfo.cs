using System;

namespace MapBoard.Extension
{
    public class RouteInfo
    {
        public PathStepInfo[] Steps { get; set; }
        public double Distance { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}