using System;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    /// <summary>
    /// 速度值转换器，从数值转为描述字符串
    /// </summary>
    public class SpeedValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "";
            }
            double speed = (double)value;
            return Convert(speed);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        internal static string Convert(double speed)
        {
            if (speed == 0)
            {
                return "";
            }
            return speed.ToString("0.00") + " m/s    " + (3.6 * speed).ToString("0.00") + " km/h";
        }
    }
}