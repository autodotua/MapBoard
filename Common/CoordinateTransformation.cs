using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Geography.CoordinateSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoPoint = NetTopologySuite.Geometries.Point;

namespace MapBoard.Common
{
    /// <summary>
    /// 坐标转换类
    /// </summary>
    public class CoordinateTransformation
    {
        /// <summary>
        /// 支持的地理坐标系统
        /// </summary>
        public static readonly string[] CoordinateSystems = new string[]
        {
            "WGS84",
            "CGCS2000",
            "GCJ02"
        };
        /// <summary>
        /// CGCS2000的SpatialReference
        /// </summary>
        public static readonly SpatialReference Cgcs2000 = new SpatialReference(4490);
        /// <summary>
        /// WGS84的SpatialReference
        /// </summary>
        public static readonly SpatialReference Wgs84 = SpatialReferences.Wgs84;

        public CoordinateTransformation(string from, string to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }
        /// <summary>
        /// 转换前的坐标系统
        /// </summary>
        public string From { get; private set; }
        /// <summary>
        /// 转换后的坐标系统
        /// </summary>
        public string To { get; private set; }

        public SpatialReference ToSpatialReference
        {
            get
            {
                switch (To)
                {
                    case "WGS84":
                        return Wgs84;
                    case "GCJ02":
                        return Wgs84;
                    case "CGCS2000":
                        return Cgcs2000;
                    default:
                        throw new Exception("未知坐标系");
                }
            }
        }

        /// <summary>
        /// 对一个要素进行坐标系统的转换，无视其内置的坐标系统的描述
        /// </summary>
        /// <param name="feature"></param>
        public void Transformate(Feature feature)
        {
            Geometry geometry = feature.Geometry;
            Geometry newGeometry = null;
            switch (feature.FeatureTable.GeometryType)
            {
                case GeometryType.Multipoint:
                    Multipoint multipoint = geometry as Multipoint;
                    newGeometry = new Multipoint(multipoint.Points.Select(p => Transformate(p)));
                    break;
                case GeometryType.Point:
                    newGeometry = Transformate(geometry as MapPoint);
                    break;
                case GeometryType.Polygon:
                    Polygon polygon = geometry as Polygon;
                    List<IEnumerable<MapPoint>> newPolygonParts = new List<IEnumerable<MapPoint>>();
                    foreach (var part in polygon.Parts)
                    {
                        IEnumerable<MapPoint> newPart = part.Points.Select(p => Transformate(p));
                        newPolygonParts.Add(newPart);
                    }
                    newGeometry = new Polygon(newPolygonParts);
                    break;
                case GeometryType.Polyline:
                    Polyline polyline = geometry as Polyline;
                    List<IEnumerable<MapPoint>> newPolylineParts = new List<IEnumerable<MapPoint>>();
                    foreach (var part in polyline.Parts)
                    {
                        IEnumerable<MapPoint> newPart = part.Points.Select(p => Transformate(p));
                        newPolylineParts.Add(newPart);
                    }
                    newGeometry = new Polyline(newPolylineParts);
                    break;
                default:
                    throw new Exception("未知类型");
            }
            feature.Geometry = newGeometry;
        }

        /// <summary>
        /// 对一个点进行坐标转换
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public MapPoint Transformate(MapPoint point)
        {
            MapPoint wgs84 = ToWgs84(point);
            return FromWgs84(wgs84);
        }
        /// <summary>
        /// 对一个点进行坐标转换
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public GeoPoint Transformate(GeoPoint point)
        {
            MapPoint wgs84 = GeoPointToWgs84(point);
            return FromWgs84GeoPoint(wgs84);
        }
        /// <summary>
        /// 对一个点进行坐标转换
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public MapPoint TransformateToMapPoint(GeoPoint point)
        {
            if (From != To)
            {
                MapPoint wgs84 = GeoPointToWgs84(point);
                GeoPoint newPoint = FromWgs84GeoPoint(wgs84);
                return new MapPoint(newPoint.X, newPoint.Y);
            }
            else
            {
                return new MapPoint(point.X, point.Y);
            }
        }
        /// <summary>
        /// 对一个点进行坐标转换
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public void TransformateSelf(GeoPoint point)
        {
            if (From != To)
            {
                MapPoint wgs84 = GeoPointToWgs84(point);
                GeoPoint newPoint = FromWgs84GeoPoint(wgs84);
                point.X = newPoint.X;
                point.Y = newPoint.Y;
            }
        }
        /// <summary>
        /// 将一个点转换到WGS84
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private MapPoint ToWgs84(MapPoint point)
        {
            switch (From)
            {
                case "WGS84":
                    return point;
                case "GCJ02":
                    GeoPoint geoPoint = new GeoPoint(point.X, point.Y);
                    GeoPoint newPoint = ChineseCoordinateTransformation.GCJ02ToWGS84(geoPoint);
                    return new MapPoint(newPoint.X, newPoint.Y, Wgs84);
                case "CGCS2000":
                    MapPoint cgcs2000Point = new MapPoint(point.X, point.Y, Cgcs2000);
                    return GeometryEngine.Project(cgcs2000Point, Wgs84) as MapPoint;
                default:
                    throw new Exception("未知坐标系");
            }
        }
        /// <summary>
        /// 将一个点从WGS84转换到其他坐标系
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private MapPoint FromWgs84(MapPoint point)
        {
            switch (To)
            {
                case "WGS84":
                    return point;
                case "GCJ02":
                    GeoPoint geoPoint = new GeoPoint(point.X, point.Y);
                    GeoPoint newPoint = ChineseCoordinateTransformation.WGS84ToGCJ02(geoPoint);
                    return new MapPoint(newPoint.X, newPoint.Y, Wgs84);
                case "CGCS2000":
                    return GeometryEngine.Project(point, Cgcs2000) as MapPoint;
                default:
                    throw new Exception("未知坐标系");
            }
        }

        /// <summary>
        /// 将一个点转换到WGS84
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private MapPoint GeoPointToWgs84(GeoPoint point)
        {

            switch (From)
            {
                case "WGS84":
                    return new MapPoint(point.X, point.Y, Wgs84);
                case "GCJ02":
                    GeoPoint newPoint = ChineseCoordinateTransformation.GCJ02ToWGS84(point);
                    return new MapPoint(newPoint.X, newPoint.Y, Wgs84);
                case "CGCS2000":
                    MapPoint arcPoint = new MapPoint(point.X, point.Y, Cgcs2000);
                    return GeometryEngine.Project(arcPoint, Wgs84) as MapPoint;
                default:
                    throw new Exception("未知坐标系");
            }
        }
        /// <summary>
        /// 将一个点从WGS84转换到其他坐标系
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private GeoPoint FromWgs84GeoPoint(MapPoint point)
        {
            switch (To)
            {
                case "WGS84":
                    return new GeoPoint(point.X, point.Y);
                case "GCJ02":
                    GeoPoint geoPoint = new GeoPoint(point.X, point.Y);
                    return ChineseCoordinateTransformation.WGS84ToGCJ02(geoPoint);
                case "CGCS2000":
                    MapPoint wgs84Point = GeometryEngine.Project(point, Wgs84) as MapPoint;
                    return new GeoPoint(wgs84Point.X, wgs84Point.Y);
                default:
                    throw new Exception("未知坐标系");
            }
        }
    }
}
