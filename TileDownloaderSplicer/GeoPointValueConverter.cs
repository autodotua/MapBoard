using FzLib.Control.Win10Style;
using FzLib.Geography.Coordinate;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.TileDownloaderSplicer
{
    public class GeoPointValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GeoPoint point = value as GeoPoint;

            return point.Latitude + "," + point.Longitude;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RangeSlider r = new RangeSlider();
            throw new NotImplementedException();
        }
    }

}
