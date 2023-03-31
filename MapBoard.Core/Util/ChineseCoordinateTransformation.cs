using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapBoard.Util
{
    /// <summary>
    /// 中国火星坐标系的转换，GCJ01、BD09和WGS84的互转
    /// </summary>
    public static class ChineseCoordinateTransformation
    {
        public static MapPoint BD09ToWgs84(MapPoint MapPoint)
        {
            var gcj = BD09ToGCJ02(MapPoint);
            var wgs84 = GCJ02ToWGS84(gcj);
            return wgs84;
        }


        public static MapPoint BD09ToGCJ02(MapPoint MapPoint)
        {
            var bd_lat = MapPoint.Y;
            var bd_lon = MapPoint.X;
            double x = bd_lon - 0.0065;
            double y = bd_lat - 0.006;
            double z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * Math.PI);
            double theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * Math.PI);
            double gg_lng = z * Math.Cos(theta);
            double gg_lat = z * Math.Sin(theta);
            return new MapPoint(gg_lng, gg_lat);
        }

        public static MapPoint WGS84ToGCJ02(MapPoint wgLoc)
        {
            double num = TransformLat(wgLoc.X - 105.0, wgLoc.Y - 35.0);
            double num2 = TransformLon(wgLoc.X - 105.0, wgLoc.Y - 35.0);
            double d = wgLoc.Y / 180.0 * 3.1415926535897931;
            double num3 = Math.Sin(d);
            num3 = 1.0 - 0.0066934216229659433 * num3 * num3;
            double num4 = Math.Sqrt(num3);
            num = num * 180.0 / (6335552.7170004258 / (num3 * num4) * 3.1415926535897931);
            num2 = num2 * 180.0 / (6378245.0 / num4 * Math.Cos(d) * 3.1415926535897931);
            return new MapPoint(wgLoc.X + num2, wgLoc.Y + num);
        }

        public static MapPoint WGS84ToBD09(MapPoint wgLoc)
        {
            wgLoc = WGS84ToGCJ02(wgLoc);
            double num = 52.359877559829883;
            double X = wgLoc.X;
            double Y = wgLoc.Y;
            double num2 = Math.Sqrt(X * X + Y * Y) + 2E-05 * Math.Sin(Y * num);
            double d = Math.Atan2(Y, X) + 3E-06 * Math.Cos(X * num);
            return new MapPoint(num2 * Math.Cos(d) + 0.0065, num2 * Math.Sin(d) + 0.006);
        }

        public static MapPoint GCJ02ToWGS84(MapPoint gcjPoint)
        {
            MapPoint latLng = Transform(gcjPoint);
            return new MapPoint(gcjPoint.X - latLng.X, gcjPoint.Y - latLng.Y);
        }

        private static double TransformLat(double x, double y)
        {
            double num = -100.0 + 2.0 * x + 3.0 * y + 0.2 * y * y + 0.1 * x * y + 0.2 * Math.Sqrt(x > 0.0 ? x : 0.0 - x);
            num += (20.0 * Math.Sin(6.0 * x * 3.1415926535897931) + 20.0 * Math.Sin(2.0 * x * 3.1415926535897931)) * 2.0 / 3.0;
            num += (20.0 * Math.Sin(y * 3.1415926535897931) + 40.0 * Math.Sin(y / 3.0 * 3.1415926535897931)) * 2.0 / 3.0;
            return num + (160.0 * Math.Sin(y / 12.0 * 3.1415926535897931) + 320.0 * Math.Sin(y * 3.1415926535897931 / 30.0)) * 2.0 / 3.0;
        }

        private static double TransformLon(double x, double y)
        {
            double num = 300.0 + x + 2.0 * y + 0.1 * x * x + 0.1 * x * y + 0.1 * Math.Sqrt(x > 0.0 ? x : 0.0 - x);
            num += (20.0 * Math.Sin(6.0 * x * 3.1415926535897931) + 20.0 * Math.Sin(2.0 * x * 3.1415926535897931)) * 2.0 / 3.0;
            num += (20.0 * Math.Sin(x * 3.1415926535897931) + 40.0 * Math.Sin(x / 3.0 * 3.1415926535897931)) * 2.0 / 3.0;
            return num + (150.0 * Math.Sin(x / 12.0 * 3.1415926535897931) + 300.0 * Math.Sin(x / 30.0 * 3.1415926535897931)) * 2.0 / 3.0;
        }

        public static bool OutOfChina(double lat, double lon)
        {
            if (lon < 72.004 || lon > 137.8347)
            {
                return true;
            }
            if (lat < 0.8293 || lat > 55.8271)
            {
                return true;
            }
            return false;
        }

        private static MapPoint Transform(MapPoint MapPoint)
        {
            double num = TransformLat(MapPoint.X - 105.0, MapPoint.Y - 35.0);
            double num2 = TransformLon(MapPoint.X - 105.0, MapPoint.Y - 35.0);
            double d = MapPoint.Y / 180.0 * 3.1415926535897931;
            double num3 = Math.Sin(d);
            num3 = 1.0 - 0.0066934216229659433 * num3 * num3;
            double num4 = Math.Sqrt(num3);
            num = num * 180.0 / (6335552.7170004258 / (num3 * num4) * 3.1415926535897931);
            num2 = num2 * 180.0 / (6378245.0 / num4 * Math.Cos(d) * 3.1415926535897931);
            return new MapPoint(num2, num);
        }
    }
}