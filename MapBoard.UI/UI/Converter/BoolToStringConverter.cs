using System;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] parts = (parameter as string).Replace("\\:", "{colon}").Split(':');
            if (parts.Length != 2)
            {
                throw new Exception("参数格式错误");
            }

            return ((bool)value ? parts[0] : parts[1]).Replace("{colon}", ":");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Two-way binding not supported by IsNotNullToBoolConverter");
        }
    }
}