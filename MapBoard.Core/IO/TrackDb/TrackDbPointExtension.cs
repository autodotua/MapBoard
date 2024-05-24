using MapBoard.IO.Gpx;
using System;

namespace MapBoard.IO.TrackDb
{
    public static class TrackDbPointExtension
    {
        public static TrackPointEntity ToTrackDbPoint(this GpxPoint point)
        {
            string value = null;
            return new TrackPointEntity()
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z,
                Time = point.Time ?? throw new ArgumentNullException(nameof(point.Time)),
                Accuracy = point.Extensions.TryGetValue(nameof(TrackPointEntity.Accuracy), out value) ? double.Parse(value) : null,
                VerticalAccuracy = point.Extensions.TryGetValue(nameof(TrackPointEntity.VerticalAccuracy), out value) ? double.Parse(value) : null,
                Speed = point.Extensions.TryGetValue(nameof(TrackPointEntity.Speed), out value) ? double.Parse(value) : null,
                Course = point.Extensions.TryGetValue(nameof(TrackPointEntity.Course), out value) ? double.Parse(value) : null,
            };
        }
        public static  GpxPoint ToGpxPoint(this TrackPointEntity point)
        {
            var gpx = new GpxPoint(point.X, point.Y, point.Z, point.Time);
            if (point.Accuracy.HasValue)
            {
                gpx.Extensions.Add(nameof(point.Accuracy), point.Accuracy.ToString());
            }
            if (point.VerticalAccuracy.HasValue)
            {
                gpx.Extensions.Add(nameof(point.VerticalAccuracy), point.VerticalAccuracy.ToString());
            }
            if (point.Course.HasValue)
            {
                gpx.Extensions.Add(nameof(point.Course), point.Course.ToString());
            }
            if (point.Speed.HasValue)
            {
                gpx.Extensions.Add(nameof(point.Speed), point.Speed.ToString());
            }
            return gpx;
        }


    }


}
