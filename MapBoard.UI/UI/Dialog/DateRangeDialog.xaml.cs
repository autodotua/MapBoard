using FzLib;
using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DateRangeDialog : CommonDialog
    {
        public DateRangeDialog(ILayerInfo layer)
        {
            From = DateTime.Now.AddYears(-1);
            To = DateTime.Now;
            Fields = layer.Fields.Where(p => p.Type == FieldInfoType.Date);
            Field = Fields.FirstOrDefault() ;
            InitializeComponent();
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = To >= From && Field != null;
        }

        public IEnumerable<FieldInfo> Fields { get; set; }
        public FieldInfo Field { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}