using System;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    /// <summary>
    /// 地图瓦片下载器的标题转换器
    /// </summary>
    public class TileDownloaderTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = (double)value;
            if (percent == 0)
            {
                return "地图瓦片下载拼接器";
            }
            return $"正在下载({percent * 100:0.0}%) - 地图瓦片下载拼接器";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}