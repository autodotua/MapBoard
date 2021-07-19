using MapBoard.Mapping.Model;
using MapBoard.Model;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    public class DrawAndSelectButtonVisiableConverter : IMultiValueConverter
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="values">0：画板是否处于就绪状态；1：选中的图层</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)values[0] && values[1] is MapLayerInfo layer)
            {
                return layer.IsLoaded && layer.LayerVisible;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}