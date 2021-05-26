using Esri.ArcGISRuntime.Geometry;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.Main.UI.Converter
{
    public class GeometryTypeDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GeometryType type)
            {
                switch (type)
                {
                    case GeometryType.Point:
                        return "点";

                    case GeometryType.Polygon:
                        return "多边形";

                    case GeometryType.Polyline:
                        return "折线";

                    case GeometryType.Multipoint:
                        return "多点";

                    default:
                        return "未知";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}