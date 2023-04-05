using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Linq;
using System.Windows.Controls;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 坐标转换对话框
    /// </summary>
    /// <remarks>
    /// 主要是为了中国加密坐标系而提供。其中CGCS2000与WGS84的差异约1mm，可以忽略不计，因此认为两者是相等的，不提供转换。
    /// </remarks>
    public partial class CoordinateTransformationDialog : CommonDialog
    {
        public CoordinateTransformationDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 所有坐标系统
        /// </summary>
        public CoordinateSystem[] CoordinateSystems { get; } = Enum.GetValues(typeof(CoordinateSystem)).Cast<CoordinateSystem>().ToArray();
        
        /// <summary>
        /// 源坐标系统
        /// </summary>
        public CoordinateSystem Source { get; set; } = CoordinateSystem.GCJ02;

        /// <summary>
        /// 目标坐标系统
        /// </summary>
        public CoordinateSystem Target { get; set; } = CoordinateSystem.WGS84;

        /// <summary>
        /// 坐标系统的选择发生改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = Source != Target;
        }
    }
}