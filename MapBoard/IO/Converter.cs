using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.IO
{
    public static class Converter
    {
        public static MapPoint GetRightCoordinateSystemPoint(FzLib.Geography.Coordinate.GeoPoint point)
        {
            if (Config.Instance.GCJ02)
            {
                point = FzLib.Geography.Coordinate.Convert.GeoCoordConverter.WGS84ToGCJ02(point);
            }

            return new MapPoint(point.Longitude, point.Latitude);
        }
        
    }
}
