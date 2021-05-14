using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Geometry;

namespace MapBoard.Main.Util
{
    public static class GeometryUtility
    {
        public static IEnumerable<Geometry> EnsureSinglePart(Geometry geometry)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    yield return geometry;
                    break;

                case GeometryType.Polyline:
                    var line = geometry as Polyline;

                    foreach (var part in line.Parts)
                    {
                        yield return new Polyline(part);
                    }
                    break;

                case GeometryType.Polygon:
                    var polygon = geometry as Polygon;

                    foreach (var part in polygon.Parts)
                    {
                        yield return new Polygon(part);
                    }
                    break;

                case GeometryType.Multipoint:
                    foreach (var point in (geometry as Multipoint).Points)
                    {
                        yield return point;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        public static Geometry RemoveZAndM(Geometry geometry)
        {
            if (!geometry.HasM && !geometry.HasZ)
            {
                return geometry;
            }
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    var point = geometry as MapPoint;
                    return new MapPoint(point.X, point.Y, geometry.SpatialReference);

                case GeometryType.Polyline:
                case GeometryType.Polygon:
                    List<IEnumerable<MapPoint>> parts = new List<IEnumerable<MapPoint>>();
                    foreach (var part in (geometry as Multipart).Parts)
                    {
                        parts.Add(part.Points.Select(p => RemoveZAndM(p) as MapPoint));
                    }
                    if (geometry.GeometryType == GeometryType.Polyline)
                    {
                        return new Polyline(parts);
                    }
                    else
                    {
                        return new Polygon(parts);
                    }

                case GeometryType.Multipoint:
                    return new Multipoint((geometry as Multipoint).Points.Select(p => RemoveZAndM(p) as MapPoint));

                default:
                    throw new NotSupportedException();
            }
        }
    }
}