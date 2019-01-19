using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Geography.Coordinate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.IO
{
    public class CoordinateTransformation
    {
        public static readonly string[] CoordinateSystems = new string[]
        {
            "WGS84",
            "CGCS2000",
            "GCJ02"
        };

        public static readonly SpatialReference Cgcs2000 = new SpatialReference(4490);
        public static readonly SpatialReference Wgs84 = SpatialReferences.Wgs84;

        public CoordinateTransformation(string from, string to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        public string From { get; private set; }
        public string To { get; private set; }


        public void Transformate(Feature feature)
        {
            Geometry geometry = feature.Geometry;
            Geometry newGeometry = null;
            switch (feature.FeatureTable.GeometryType)
            {
                case GeometryType.Multipoint:
                    Multipoint multipoint = geometry as Multipoint;
                    newGeometry =new Multipoint(multipoint.Points.Select(p => Transformate(p)));
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

        private MapPoint Transformate(MapPoint point)
        {
            MapPoint wgs84 = ToWgs84(point);
            return FromWgs84(wgs84);
        }
        private MapPoint ToWgs84(MapPoint point)
        {
            switch (From)
            {
                case "WGS84":
                    return point;
                case "GCJ02":
                    GeoPoint geoPoint = new GeoPoint(point.Y, point.X);
                    GeoPoint newPoint = FzLib.Geography.Coordinate.Convert.GeoCoordConverter.GCJ02ToWGS84(geoPoint);
                    return new MapPoint(newPoint.Longitude, newPoint.Latitude,Wgs84);
                case "CGCS2000":
                    MapPoint cgcs2000Point = new MapPoint(point.X, point.Y, Cgcs2000);
                    return GeometryEngine.Project(cgcs2000Point, Wgs84) as MapPoint;
                default:
                    throw new Exception("未知坐标系");
            }
        }

        private MapPoint FromWgs84(MapPoint point)
        {
            switch (To)
            {
                case "WGS84":
                    return point;
                case "GCJ02":
                    GeoPoint geoPoint = new GeoPoint(point.Y, point.X);
                    GeoPoint newPoint = FzLib.Geography.Coordinate.Convert.GeoCoordConverter.WGS84ToGCJ02(geoPoint);
                    return new MapPoint(newPoint.Longitude, newPoint.Latitude,Wgs84);
                case "CGCS2000":
                    return GeometryEngine.Project(point, Cgcs2000) as MapPoint;
                default:
                    throw new Exception("未知坐标系");
            }
        }


    }
}
