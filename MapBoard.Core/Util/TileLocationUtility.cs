using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Math;

namespace MapBoard.Util
{
    public static class TileLocationUtility
    {
        /// <summary>
        /// 从经纬度获取某一级别瓦片坐标编号
        /// </summary>
        /// <param name="p"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static (int x, int y) GeoPointToTile(MapPoint p, int level)
        {
            return PointToTile(p.Y, p.X, level);
        }

        /// <summary>
        /// 将纬度和经度坐标转换为给定缩放级别的Slippy Map瓦片坐标
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static (int x, int y) GetSlippyMapTile(double lat, double lng, int level)
        {
            // 计算经度坐标相对于地图宽度的比例
            var x = (lng + 180) / 360;
            // 计算瓦片的X坐标
            int tileX = (int)(x * Pow(2, level));

            // 将纬度坐标转换为弧度
            var lat_rad = lat * PI / 180;
            // 计算纬度坐标相对于地图高度的比例
            var y = 0.5 - Log(Tan(lat_rad) + 1 / Cos(lat_rad)) / (2 * PI);
            // 计算瓦片的Y坐标
            int tileY = (int)(y * Pow(2, level));

            // 返回包含瓦片X和Y坐标的元组
            return (tileX, tileY);
        }

        /// <summary>
        /// 从某一瓦片的某一像素点到经纬度
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static (double lat, double lng) PixelToGeoPoint(int pixelX, int pixelY, int tileX, int tileY, int level)
        {
            var lng = PixelXTolng(pixelX, tileX, level);
            var lat = PixelYToLat(pixelY, tileY, level);

            return (lat, lng);
        }

        /// <summary>
        /// 从经纬度获取点在某一级别瓦片中的像素坐标
        /// </summary>
        /// <param name="p"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static (int x, int y) PointToPixel(MapPoint p, int level)
        {
            return PointToPixel(p.Y, p.X, level);
        }

        /// <summary>
        /// 从经纬度获取点在某一级别瓦片中的像素坐标
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static (int x, int y) PointToPixel(double latitude, double longitude, int level)
        {
            var pixelX = lngToPixelX(longitude, level);
            var pixelY = latToPixelY(latitude, level);

            return (pixelX, pixelY);
        }

        /// <summary>
        /// 找到某个经纬度的点在某一缩放等级下瓦片的位置
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static (int x, int y) PointToTile(double latitude, double longitude, int level)
        {
            var tileX = LngToTileX(longitude, level);
            var tileY = LatToTileY(latitude, level);
            return (tileX, tileY);
        }

        /// <summary>
        /// 某一瓦片等级下瓦片地图X轴(Y轴)上的瓦片数目
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static double GetMapSize(double level)
        {
            return Pow(2, level);
        }

        /// <summary>
        /// 分辨率，表示水平方向上一个像素点代表的真实距离(m)
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static double GetResolution(double latitude, int level)
        {
            var resolution = 6378137.0 * 2 * PI * Cos(latitude) / 256 / GetMapSize(level);
            return resolution;
        }

        /// <summary>
        /// 找到某个缩放等级下经度对应的瓦片纵坐标
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static int latToPixelY(double latitude, int level)
        {
            var sinLatitude = Sin(latitude * PI / 180);
            var y = 0.5 - Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * PI);
            var pixelY = (int)(y * GetMapSize(level) * 256 % 256);

            return pixelY;
        }

        private static int LatToTileY(double latitude, int level)
        {
            var lat_rad = latitude * PI / 180;
            var y = (1 - Log(Tan(lat_rad) + 1 / Cos(lat_rad)) / PI) / 2;
            var tileY = (int)(y * GetMapSize(level));

            // 代替性算法,使用了一些三角变化，其实完全等价
            //var sinLatitude = Sin(latitude * PI / 180);
            //var y = 0.5 - Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * PI);
            //var tileY = (int)(y * _getMapSize(level));

            return tileY;
        }

        /// <summary>
        /// 找到某个缩放等级下经度对应的瓦片横坐标
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static int lngToPixelX(double longitude, int level)
        {
            var x = (longitude + 180) / 360;
            var pixelX = (int)(x * GetMapSize(level) * 256 % 256);

            return pixelX;
        }

        private static int LngToTileX(double longitude, int level)
        {
            var x = (longitude + 180) / 360;
            var tileX = (int)(x * GetMapSize(level));
            return tileX;
        }
        /// <summary>
        /// 根据缩放等级，瓦片的横坐标以及上面像素的横坐标转换为经度
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="tileX"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static double PixelXTolng(int pixelX, int tileX, int level)
        {
            var pixelXToTileAddition = pixelX / 256.0;
            var lngitude = (tileX + pixelXToTileAddition) / GetMapSize(level) * 360 - 180;

            return lngitude;
        }

        /// <summary>
        /// 根据缩放等级，瓦片的纵坐标以及上面像素的纵坐标转换为纬度
        /// </summary>
        /// <param name="pixelY"></param>
        /// <param name="tileY"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static double PixelYToLat(int pixelY, int tileY, int level)
        {
            var pixelYToTileAddition = pixelY / 256.0;
            var latitude = Atan(Sinh(PI * (1 - 2 * (tileY + pixelYToTileAddition) / GetMapSize(level)))) * 180.0 / PI;

            return latitude;
        }
    }
}