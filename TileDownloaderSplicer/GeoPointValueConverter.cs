using FzLib.UI.Win10Style;
using GIS.Geometry;
using NetTopologySuite.Geometries;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.TileDownloaderSplicer
{
    public class GeoPointValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point point = value as Point;

            return point.X + "," + point.Y;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RangeSlider r = new RangeSlider();
            throw new NotImplementedException();
        }
    }

}
