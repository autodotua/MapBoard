using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MapBoard.Util
{
    public static class LocationMenuUtility
    {
        public static string GetLocationMenuString(MapPoint location)
        {
            if ((location.SpatialReference?.Wkid ?? 4326) != 4326)
            {
                location = location.ToWgs84();
            }
            return $"经度：{$"{location.X,10:0.000000}"}{Environment.NewLine}纬度：{location.Y,11:0.000000}";
        }
        public static string GetLocationClipboardString(MapPoint location)
        {
            if ((location.SpatialReference?.Wkid ?? 4326) != 4326)
            {
                location = location.ToWgs84();
            }
            return Config.Instance.LocationClipboardFormat
                .Replace("{经度}", location.X.ToString("0.000000"))
                .Replace("{Longitude}", location.X.ToString("0.000000"))
                .Replace("{Lon}", location.X.ToString("0.000000"))
                .Replace("{Lng}", location.X.ToString("0.000000"))
                .Replace("{纬度}", location.Y.ToString("0.000000"))
                .Replace("{Latitude}", location.Y.ToString("0.000000"))
                .Replace("{Lat}", location.Y.ToString("0.000000"))
                .Replace("\\n", Environment.NewLine)
                .Replace("\\t", "\t");
        }
    }
}
