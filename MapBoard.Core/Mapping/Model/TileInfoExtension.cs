using MapBoard.Model;
using MapBoard.Util;
using System;
using System.Text;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 瓦片扩展方法
    /// </summary>
    public static class TileInfoExtension
    {
        /// <summary>
        /// 获取瓦片的边界信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="widthSize"></param>
        /// <param name="heightSize"></param>
        /// <returns></returns>
        public static GeoRect<double> GetExtent(this TileInfo info, int widthSize, int heightSize)
        {
            (double yMax, double xMin) = TileLocationUtility.PixelToGeoPoint(0, 0, info.X, info.Y, info.Level);
            // var prj = GeometryEngine.Project(new MapPoint(lat, lng, SpatialReferences.Wgs84), SpatialReferences.WebMercator) as MapPoint;
            (double yMin, double xMax) = TileLocationUtility.PixelToGeoPoint(widthSize, heightSize, info.X, info.Y, info.Level);

            return new GeoRect<double>(xMin, xMax, yMin, yMax);
        }
    }
}