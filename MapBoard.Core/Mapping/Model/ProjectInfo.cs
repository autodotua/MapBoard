using Esri.ArcGISRuntime.Geometry;
using MapBoard.Util;
using System;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 由瓦片合成的栅格图形对应的投影信息
    /// </summary>
    public class ProjectInfo
    {
        public ProjectInfo(int level, int x, int y)
        {
            (double lat, double lng) = TileLocationUtility.PixelToGeoPoint(0, 0, x, y, level);

            XPerPixel = 156543.034 / Math.Pow(2, level);
            YPerPixel = -156543.034 / Math.Pow(2, level);

            var p = Wgs84ToWebM(lat, lng);
            LeftTopY = p.Y;
            LeftTopX = p.X;
        }

        /// <summary>
        /// 左上角的X坐标
        /// </summary>
        public double LeftTopX { get; set; }

        /// <summary>
        /// 左上角的Y坐标
        /// </summary>
        public double LeftTopY { get; set; }

        /// <summary>
        /// 每个像素的X大小（米）
        /// </summary>
        public double XPerPixel { get; set; }

        /// <summary>
        /// 每个像素的Y大小（米）
        /// </summary>
        public double YPerPixel { get; set; }

        /// <summary>
        /// 获取投影文件内容
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $@"{XPerPixel}
0
0
{YPerPixel}
{LeftTopX}
{LeftTopY}";
        }

        /// <summary>
        /// 从WGS84坐标的经纬度转换为Web墨卡托
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <returns></returns>
        private MapPoint Wgs84ToWebM(double lat, double lng)
        {
            MapPoint p = new MapPoint(lng, lat, SpatialReferences.Wgs84);
            return GeometryEngine.Project(p, SpatialReferences.WebMercator) as MapPoint;
        }
    }
}