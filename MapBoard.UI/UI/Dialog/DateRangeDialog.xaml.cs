using FzLib;
using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DateRangeDialog : CommonDialog
    {
        private ILayerInfo Layer { get; }

        public DateRangeDialog(ILayerInfo layer)
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
                this.Notify(nameof(EnableDateRange));
                IsPrimaryButtonEnabled = !EnableDateRange || dateFrom.SelectedDate.HasValue && dateTo.SelectedDate.HasValue && dateTo.SelectedDate > dateFrom.SelectedDate;
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = !EnableDateRange
                || dateFrom.SelectedDate.HasValue && dateTo.SelectedDate.HasValue
                && dateTo.SelectedDate >= dateFrom.SelectedDate;
        }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            Layer.TimeExtent = new TimeExtentInfo()
            {
                IsEnable = EnableDateRange,
                From = dateFrom.SelectedDate.Value,
                To = dateTo.SelectedDate.Value,
            };
        }
    }
}