using Esri.ArcGISRuntime.Geometry;
using MapBoard.IO.Tile;
using System;

namespace MapBoard.Mapping.Model
{
    public class ProjectInfo
    {
        public ProjectInfo(int level, int x, int y)
        {
            (double lat, double lng) leftTop = TileLocation.PixelToGeoPoint(0, 0, x, y, level);

            XPerPixel = 156543.034 / Math.Pow(2, level);
            YPerPixel = -156543.034 / Math.Pow(2, level);

            var p = Wgs84ToWebM(leftTop.lat, leftTop.lng);
            LeftTopY = p.Y;
            LeftTopX = p.X;
        }

        private MapPoint Wgs84ToWebM(double lat, double lng)
        {
            MapPoint p = new MapPoint(lng, lat, SpatialReferences.Wgs84);
            return GeometryEngine.Project(p, SpatialReferences.WebMercator) as MapPoint;
        }

        public double XPerPixel { get; set; }
        public double YPerPixel { get; set; }
        public double LeftTopY { get; set; }
        public double LeftTopX { get; set; }

        public override string ToString()
        {
            return $@"{XPerPixel}
0
0
{YPerPixel}
{LeftTopX}
{LeftTopY}";
        }
    }
}