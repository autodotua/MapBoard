using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Math;

namespace MapBoard.IO.Tile
{
    public static class TileLocation
    {
        public static (int x, int y) GetSlippyMapTile(double lat, double lng, int level)
        {
            var x = (lng + 180) / 360;
            int tileX = (int)(x * Pow(2, level));

            var lat_rad = lat * PI / 180;
            var y = (0.5 - Log(Tan(lat_rad) + 1 / Cos(lat_rad)) / (2 * PI));
            int tileY = (int)(y * Pow(2, level));
            return (tileX, tileY);
        }

        /*
   * 某一瓦片等级下瓦片地图X轴(Y轴)上的瓦片数目
   */

        private static double GetMapSize(double level)
        {
            return Pow(2, level);
        }

        /*
         * 分辨率，表示水平方向上一个像素点代表的真实距离(m)
         */

        private static double GetResolution(double latitude, int level)
        {
            var resolution = 6378137.0 * 2 * PI * Cos(latitude) / 256 / GetMapSize(level);
            return resolution;
        }

        private static int LngToTileX(double longitude, int level)
        {
            var x = (longitude + 180) / 360;
            var tileX = (int)(x * GetMapSize(level));
            return tileX;
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

        /*
         * 从经纬度获取某一级别瓦片坐标编号
         */

        public static (int x, int y) GeoPointToTile(MapPoint p, int level)
        {
            return PointToTile(p.Y, p.X, level);
        }

        public static (int x, int y) PointToTile(double latitude, double longitude, int level)
        {
            var tileX = LngToTileX(longitude, level);
            var tileY = LatToTileY(latitude, level);
            return (tileX, tileY);
        }

        private static int _lngToPixelX(double longitude, int level)
        {
            var x = (longitude + 180) / 360;
            var pixelX = (int)(x * GetMapSize(level) * 256 % 256);

            return pixelX;
        }

        private static int _latToPixelY(double latitude, int level)
        {
            var sinLatitude = Sin(latitude * PI / 180);
            var y = 0.5 - Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * PI);
            var pixelY = (int)(y * GetMapSize(level) * 256 % 256);

            return pixelY;
        }

        /*
         * 从经纬度获取点在某一级别瓦片中的像素坐标
         */

        public static (int x, int y) PointToPixel(MapPoint p, int level)
        {
            return PointToPixel(p.Y, p.X, level);
        }

        public static (int x, int y) PointToPixel(double latitude, double longitude, int level)
        {
            var pixelX = _lngToPixelX(longitude, level);
            var pixelY = _latToPixelY(latitude, level);

            return (pixelX, pixelY);
        }

        private static double PixelXTolng(int pixelX, int tileX, int level)
        {
            var pixelXToTileAddition = pixelX / 256.0;
            var lngitude = (tileX + pixelXToTileAddition) / GetMapSize(level) * 360 - 180;

            return lngitude;
        }

        private static double PixelYToLat(int pixelY, int tileY, int level)
        {
            var pixelYToTileAddition = pixelY / 256.0;
            var latitude = Atan(Sinh(PI * (1 - 2 * (tileY + pixelYToTileAddition) / GetMapSize(level)))) * 180.0 / PI;

            return latitude;
        }

        /*
         * 从某一瓦片的某一像素点到经纬度
         */

        public static (double lat, double lng) PixelToGeoPoint(int pixelX, int pixelY, int tileX, int tileY, int level)
        {
            var lng = PixelXTolng(pixelX, tileX, level);
            var lat = PixelYToLat(pixelY, tileY, level);

            return (lat, lng);
        }
    }
}