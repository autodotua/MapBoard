using MapBoard.Common;
using ModernWpf.FzExtension.CommonDialog;
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

        /// <summary>
        /// 支持的坐标系
        /// </summary>
        public string[] CoordinateSystems => CoordinateTransformation.CoordinateSystems;

        public string SelectedCoordinateSystem1 { get; set; } = "GCJ02";
        public string SelectedCoordinateSystem2 { get; set; } = "WGS84";

        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            IsPrimaryButtonEnabled = SelectedCoordinateSystem1 != SelectedCoordinateSystem2;
        }
    }
}