using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    /// <summary>
    /// 根据绑定的<see cref="bool"/>值，选取指定的网格长度。
    /// </summary>
    /// <remarks>
    /// 长度值通过parameter提供，格式为“[s|a][true时的长度],[s|a][false时的长度]”。s表示Star，a表示Auto。Auto时不需要提供后面的值。
    /// </remarks>
    public class Bool2GridLengthConverter : IValueConverter
    {
        private static readonly Regex rParam = new Regex(@"(?<type1>[sa]?)(?<length1>[0-9\.]*),(?<type2>[sa]?)(?<length2>[0-9\.]*)", RegexOptions.Compiled);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GridLength gridLength;
            if (parameter is not string str)
            {
                throw new ArgumentException("参数必须为字符串", nameof(parameter));
            }
            if (value is not bool b)
            {
                throw new ArgumentException("值必须为bool", nameof(value));
            }
            try
            {
                int i = b ? 1 : 2;
                var match = rParam.Match(str);
                var type = match.Groups[$"type{i}"].Value;
                var length = double.Parse(match.Groups[$"length{i}"].Value);
                if (type == "a")
                {
                    return GridLength.Auto;
                }
                else if (type == "s")
                {
                    return new GridLength(length, GridUnitType.Star);
                }
                return new GridLength(length);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("参数格式错误", nameof(parameter), ex);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}