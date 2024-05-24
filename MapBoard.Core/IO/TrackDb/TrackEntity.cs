using Microsoft.EntityFrameworkCore;
using System;

namespace MapBoard.IO.TrackDb
{
    [Index(nameof(StartTime))]
    public class TrackEntity : EntityBase
    {
        public DateTime StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public double Distance {  get; set; }

        public int PointCount {  get; set; }
    }
}
