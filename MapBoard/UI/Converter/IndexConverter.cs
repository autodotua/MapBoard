using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace MapBoard.Main.UI.Converter
{
    /// <summary>
    /// 为ListViewItem提供显示索引支持
    /// </summary>
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            ListViewItem item = (ListViewItem)value;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item);
            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}