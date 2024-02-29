using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Converters
{
    public class DistanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(value is double);
            var d = (double)value;
            if (d < 1000)
            {
                return $"{Math.Round(d)} m";
            }
            return $"{Math.Round(d / 1000, 2)} km";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
