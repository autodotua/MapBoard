using MapBoard.Common;
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
using static MapBoard.Common.CoordinateTransformation;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CoordinateTransformationDialog : DialogWindowBase
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
        /// <summary>
        /// 单击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            btn.IsEnabled = SelectedCoordinateSystem1 != SelectedCoordinateSystem2;
        }
    }
}
