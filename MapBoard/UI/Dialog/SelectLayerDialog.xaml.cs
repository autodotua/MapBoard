using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using ModernWpf.FzExtension.CommonDialog;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectLayerDialog : CommonDialog
    {
        private bool canSelect = false;

        public SelectLayerDialog(MapLayerCollection layers)
        {
            InitializeComponent();
            var list = layers.Where(p => p.Table.GeometryType == layers.Selected.Table.GeometryType && p != layers.Selected);
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