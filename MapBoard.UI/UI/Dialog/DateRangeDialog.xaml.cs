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
        public DateRangeDialog()
        {
            InitializeComponent();
            From = DateTime.Now.AddYears(-1);
            To = DateTime.Now;
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = To >= From;
        }

        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}