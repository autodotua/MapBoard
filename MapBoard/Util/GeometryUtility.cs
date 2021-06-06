using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Geometry;

namespace MapBoard.Main.Util
{
    public static class GeometryUtility
    {
        /// <summary>
        /// 确保一个图形是简单的单部分图形
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static IEnumerable<Geometry> EnsureSinglePart(this Geometry geometry)
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

        /// <summary>
        /// 如果图形存在M或Z，那么移除
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static Geometry RemoveZAndM(this Geometry geometry)
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

        /// <summary>
        /// 获取米为单位的长度
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static double GetLength(this Geometry geometry)
        {
            return GeometryEngine.LengthGeodetic(geometry, null, GeodeticCurveType.NormalSection);
        }

        /// <summary>
        /// 获取平方米为单位的面积
        /// </summary>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static double GetArea(this Geometry geometry)
        {
            return GeometryEngine.AreaGeodetic(geometry, null, GeodeticCurveType.NormalSection);
        }

        public static double GetDistance(MapPoint p1, MapPoint p2)
        {
            return GeometryEngine.DistanceGeodetic(p1, p2, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.NormalSection).Distance;
        }
    }
}