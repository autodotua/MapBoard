using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common.Dialog;
using MapBoard.Main.Model;
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
        private LayerInfo Layer { get; }

        public DateRangeDialog(LayerInfo layer)
        {
            Layer = layer;
            InitializeComponent();
            if (layer.TimeExtent == null)
            {
                EnableDateRange = false;
                dateFrom.SelectedDate = DateTime.Now.AddYears(-1);
                dateTo.SelectedDate = DateTime.Now;
            }
            else
            {
                EnableDateRange = true;
                EnableDateRange = layer.TimeExtent.IsEnable;
                dateFrom.SelectedDate = layer.TimeExtent.From.Date;
                dateTo.SelectedDate = layer.TimeExtent.To.Date;
            }
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Layer.TimeExtent = new TimeExtentInfo()
            {
                IsEnable = EnableDateRange,
                From = dateFrom.SelectedDate.Value,
                To = dateTo.SelectedDate.Value,
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
                btn.IsEnabled = !EnableDateRange || dateFrom.SelectedDate.HasValue && dateTo.SelectedDate.HasValue && dateTo.SelectedDate > dateFrom.SelectedDate;
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            btn.IsEnabled = !EnableDateRange || dateFrom.SelectedDate.HasValue && dateTo.SelectedDate.HasValue && dateTo.SelectedDate > dateFrom.SelectedDate;
        }
    }
}