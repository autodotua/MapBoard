using FzLib.UI.Dialog;
using MapBoard.Common.Dialog;
using MapBoard.Main.Model;
using ModernWpf.FzExtension.CommonDialog;
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
    public partial class SelectLayerDialog : CommonDialog
    {
        private bool canSelect = false;

        public SelectLayerDialog()
        {
            InitializeComponent();
            var layers = LayerCollection.Instance;
            var list = LayerCollection.Instance.Where(p => p.Table.GeometryType == layers.Selected.Table.GeometryType && p != layers.Selected);
            if (list.Count() > 0)
            {
                lbx.ItemsSource = list;
                lbx.SelectedIndex = 0;
                canSelect = true;
            }
        }

        public LayerInfo SelectedLayer { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (canSelect == false)
            {
                Content = new TextBlock()
                {
                    Text = "没有可选择的图层",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
            }
        }
    }
}