using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common.Dialog;
using MapBoard.Main.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DateRangeDialog : Common.Dialog.DialogWindowBase
    {
        bool canSelect = false;
        StyleInfo CurrentStyle { get; }
        public DateRangeDialog(StyleInfo style)
        {
            CurrentStyle = style;
            InitializeComponent();
            if (style.TimeExtent == null)
            {
                EnableDateRange = true;
                date.DateFrom = DateTime.Now.AddYears(-1);
                date.DateTo = DateTime.Now;
            }
            else
            {
                EnableDateRange = style.TimeExtent.IsEnable;
                date.DateFrom = style.TimeExtent.From.Date;
                date.DateTo = style.TimeExtent.To.Date;
            }
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            CurrentStyle.TimeExtent = new TimeExtentInfo()
            {
                IsEnable = EnableDateRange,
                From = date.DateFrom.Value,
                To = date.DateTo.Value,
            };
            DialogResult = true;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private bool enableDateRange;
        public bool EnableDateRange
        {
            get => enableDateRange;
            set
            {
                enableDateRange = value;
                Notify(nameof(EnableDateRange));
                btn.IsEnabled = !EnableDateRange || date.DateFrom.HasValue && date.DateTo.HasValue;

            }
        }

        private void date_DateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btn.IsEnabled = !EnableDateRange || date.DateFrom.HasValue && date.DateTo.HasValue;
        }
    }
}
