using MapBoard.Mapping.Model;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace MapBoard.UI.Converter
{
    /// <summary>
    /// 要素历史记录的要素操作描述
    /// </summary>
    public class FeaturesHistoryDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string desc = null;
            if (value is not FeaturesChangedEventArgs e)
            {
                return desc;
            }
            int count = 0;
            if (e.DeletedFeatures != null)
            {
                count++;
                desc = $"删除了 {e.DeletedFeatures.Count} 个要素";
            }
            if (e.AddedFeatures != null)
            {
                count++;
                desc = $"添加了 {e.AddedFeatures.Count} 个要素";
            }
            if (e.UpdatedFeatures != null)
            {
                count++;
                desc = $"更新了 {e.UpdatedFeatures.Count} 个要素";
            }
            Debug.Assert(count == 1);
            return desc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}