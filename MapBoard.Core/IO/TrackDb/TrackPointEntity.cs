using EGIS.ShapeFileLib;
using Microsoft.EntityFrameworkCore;
using System;

namespace MapBoard.IO.TrackDb
{
    [Index(nameof(TrackId))]
    public class TrackPointEntity : EntityBase
    {
        public int TrackId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double? Z { get; set; }
        public DateTime Time { get; set; }
        public double? Accuracy { get; set; }
        public double? VerticalAccuracy { get; set; }
        public double? Course { get; set; }
        public double? Speed { get; set; }
    }


}
