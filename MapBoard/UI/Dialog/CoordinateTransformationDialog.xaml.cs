using MapBoard.Common;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Linq;
using System.Windows.Controls;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CoordinateTransformationDialog : CommonDialog
    {
        public CoordinateTransformationDialog()
        {
            InitializeComponent();
        }

        public CoordinateSystem[] CoordinateSystems { get; } = Enum.GetValues(typeof(CoordinateSystem)).Cast<CoordinateSystem>().ToArray();
        public CoordinateSystem Source { get; set; } = CoordinateSystem.GCJ02;

        public CoordinateSystem Target { get; set; } = CoordinateSystem.WGS84;

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = Source != Target;
        }
    }
}