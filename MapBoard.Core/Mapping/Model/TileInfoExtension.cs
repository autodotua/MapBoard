using MapBoard.IO.Tile;
using MapBoard.Model;
using System;
using System.Text;

namespace MapBoard.Mapping.Model
{
    public static class TileInfoExtension
    {
        public static GeoRect<double> GetExtent(this TileInfo info, int widthSize, int heightSize)
        {
            (double yMax, double xMin) = TileLocation.PixelToGeoPoint(0, 0, info.X, info.Y, info.Level);
            // var prj = GeometryEngine.Project(new MapPoint(lat, lng, SpatialReferences.Wgs84), SpatialReferences.WebMercator) as MapPoint;
            (double yMin, double xMax) = TileLocation.PixelToGeoPoint(widthSize, heightSize, info.X, info.Y, info.Level);

            return new GeoRect<double>(xMin, xMax, yMin, yMax);
        }
    }
}