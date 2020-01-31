using MapBoard.Common;
using MapBoard.Common.Dialog;
using MapBoard.Main.Layer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

//未使用
namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MultiLayersOperationDialog : DialogWindowBase
    {
        public MultiLayersOperationDialog()
        {
            Layers = new ObservableCollection<Layerselection>(LayerCollection.Instance.Layers.Select(p => new Layerselection() { Select = false, Style = p }));

            InitializeComponent();
        }
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

        public ObservableCollection<Layerselection> Layers { get; }
        public class Layerselection
        {
            public bool Select { get; set; }
            public LayerInfo Style { get; set; }
        }
    }
}
