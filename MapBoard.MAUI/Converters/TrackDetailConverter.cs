using MapBoard.Model;
using MapBoard.Services;
using System.Globalization;

namespace MapBoard.Converters;

public class TrackDetailConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value== null)
        {
            return "未知";
        }
        return parameter.ToString() switch
        {
            "0" => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"),
            "2" => (double)value > 1000 ? $"{(double)value / 1000:0.00} km" : $"{(double)value:0} m",
            "3" => $"{GetLongitudeString(value as Location)} {GetLatitudeString(value as Location)}",
            "4" => (value as Location).Altitude.HasValue ? $"{(value as Location).Altitude.Value:0.0} m" : "未知",
            "5" => (value as Location).Speed.HasValue ? $"{(value as Location).Speed:0.0} m/s   {(value as Location).Speed / 3.6:0.0} km/h" : "未知",
            "6" => (value as Location).Course.HasValue ? $"{(value as Location).Course:0.0}°  {Angle2Direction((value as Location).Course.Value)}" : "未知",
            "7" => (value as Location).Accuracy.HasValue && (value as Location).VerticalAccuracy.HasValue ? $"{(value as Location).Accuracy:0.0} m (H)  {(value as Location).VerticalAccuracy:0.0} m (V)" : "未知",
            "8" => $"{(value as GnssStatusInfo).Fixed} / {(value as GnssStatusInfo).Total}",
            _ => throw new NotImplementedException(),
        };
    }

    private string GetLatitudeString(Location location)
    {
        if (location.Latitude >= 0)
        {
            return $"{location.Latitude:0.00000}°N";
        }
        return $"{-location.Latitude:0.00000}°S";
    }
    private string GetLongitudeString(Location location)
    {
        if (location.Longitude >= 0)
        {
            return $"{location.Longitude:0.00000}°E";
        }
        return $"{-location.Longitude:0.00000}°W";
    }

    private string Angle2Direction(double angle)
    {
        angle += 11.25;
        string[] types = ["北", "北偏东", "东北", "东偏北", "东", "东偏南", "东南", "南偏东", "南", "南偏西", "西南", "西偏南", "西", "西偏北", "西北", "北偏西", "北"];
        return types[(int)(angle / 22.5)];
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
