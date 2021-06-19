using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapBoard.Util
{
    /// <summary>
    /// 坐标转换类
    /// </summary>
    public static class CoordinateTransformation
    {
        /// <summary>
        /// 对一个图形进行坐标系统的转换，无视其内置的坐标系统的描述
        /// </summary>
        /// <param name="feature"></param>

        public static Geometry Transformate(Geometry geometry, CoordinateSystem source, CoordinateSystem target)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Multipoint:
                    Multipoint multipoint = geometry as Multipoint;
                    return new Multipoint(multipoint.Points.Select(p => Transformate(p, source, target)));

                case GeometryType.Point:
                    return Transformate(geometry as MapPoint, source, target);

                case GeometryType.Polygon:
                    Polygon polygon = geometry as Polygon;
                    List<IEnumerable<MapPoint>> newPolygonParts = new List<IEnumerable<MapPoint>>();
                    foreach (var part in polygon.Parts)
                    {
                        IEnumerable<MapPoint> newPart = part.Points.Select(p => Transformate(p, source, target));
                        newPolygonParts.Add(newPart);
                    }
                    return new Polygon(newPolygonParts);

                case GeometryType.Polyline:
                    Polyline polyline = geometry as Polyline;
                    List<IEnumerable<MapPoint>> newPolylineParts = new List<IEnumerable<MapPoint>>();
                    foreach (var part in polyline.Parts)
                    {
                        IEnumerable<MapPoint> newPart = part.Points.Select(p => Transformate(p, source, target));
                        newPolylineParts.Add(newPart);
                    }
                    return new Polyline(newPolylineParts);

                case GeometryType.Envelope:
                    Envelope rect = geometry as Envelope;
                    MapPoint leftTop = new MapPoint(rect.XMin, rect.YMax, rect.SpatialReference);
                    MapPoint rightBottom = new MapPoint(rect.XMax, rect.YMin, rect.SpatialReference);
                    MapPoint newLeftTop = Transformate(leftTop, source, target);
                    MapPoint newRightBottom = Transformate(rightBottom, source, target);
                    return new Envelope(newLeftTop, newRightBottom);

                default:
                    throw new Exception("未知类型");
            }
        }

        /// <summary>
        /// 对一个点进行坐标转换
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static MapPoint Transformate(MapPoint point, CoordinateSystem source, CoordinateSystem target)
        {
            if (source == target)
            {
                return point;
            }
            MapPoint wgs84 = ToWgs84(point, source);
            return FromWgs84(wgs84, target);
        }

        /// <summary>
        /// 将一个点转换到WGS84
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private static MapPoint ToWgs84(MapPoint point, CoordinateSystem source)
        {
            switch (source)
            {
                case CoordinateSystem.WGS84:
                    return point;

                case CoordinateSystem.GCJ02:
                    {
                        return ChineseCoordinateTransformation.GCJ02ToWGS84(point);
                    }
                case CoordinateSystem.BD09:
                    {
                        return ChineseCoordinateTransformation.BD09ToWgs84(point);
                    }
                default:
                    throw new Exception("未知坐标系");
            }
        }

        /// <summary>
        /// 将一个点从WGS84转换到其他坐标系
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private static MapPoint FromWgs84(MapPoint point, CoordinateSystem target)
        {
            switch (target)
            {
                case CoordinateSystem.WGS84:
                    return point;

                case CoordinateSystem.GCJ02:
                    {
                        return ChineseCoordinateTransformation.WGS84ToGCJ02(point);
                    }
                case CoordinateSystem.BD09:
                    {
                        return ChineseCoordinateTransformation.WGS84ToBD09(point);
                    }
                default:
                    throw new Exception("未知坐标系");
            }
        }
    }
}