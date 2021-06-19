using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    /// <summary>
    /// 通过参数将enum转换为bool。
    /// 参数格式示例：Pausing/Stop/Start:false
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] parts = (parameter as string).Split(':');

            if (parts.Length != 2)
            {
                throw new Exception("参数格式错误");
            }
            string key = parts[0];
            if (!bool.TryParse(parts[1], out bool b))
            {
                throw new Exception("布尔值错误");
            }
            if (key.Split('/').Contains(value.ToString()))
            {
                return b;
            }
            else
            {
                return !b;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Two-way binding not supported by IsNotNullToBoolConverter");
        }
    }
}