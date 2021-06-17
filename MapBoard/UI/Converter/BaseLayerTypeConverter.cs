using MapBoard.Common.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.Main.UI.Converter
{
    public class BaseLayerTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (BaseLayerType)value switch
            {
                BaseLayerType.RasterLayer => "栅格图",
                BaseLayerType.TpkLayer => "切片包",
                BaseLayerType.ShapefileLayer => "Shapefile矢量图",
                BaseLayerType.WebTiledLayer => "网络瓦片图",
                _ => throw new NotSupportedException()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}