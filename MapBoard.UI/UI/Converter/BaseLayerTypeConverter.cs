using MapBoard.Model;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    public class BaseLayerTypeConverter : IValueConverter
    {
        public static string GetName(BaseLayerType type)
        {
            return type switch
            {
                BaseLayerType.RasterLayer => "栅格图",
                BaseLayerType.TpkLayer => "切片包",
                BaseLayerType.ShapefileLayer => "Shapefile",
                BaseLayerType.WebTiledLayer => "XYZ瓦片图",
                BaseLayerType.WmsLayer => "WMS",
                BaseLayerType.WmtsLayer => "WMTS",
                _ => type.ToString()
            };
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetName((BaseLayerType)value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}