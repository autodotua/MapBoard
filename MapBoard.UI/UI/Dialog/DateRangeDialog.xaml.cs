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
    /// 定义日期范围字段
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

        /// <summary>
        /// 选取的日期字段
        /// </summary>
        public FieldInfo Field { get; set; }

        /// <summary>
        /// 图层所有字段
        /// </summary>
        public IEnumerable<FieldInfo> Fields { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime From { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime To { get; set; }

        private void DateField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = To >= From && Field != null;
        }
    }
}