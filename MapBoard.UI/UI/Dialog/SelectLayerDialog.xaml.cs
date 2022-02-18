using MapBoard.Model;
using MapBoard.Mapping;
using ModernWpf.FzExtension.CommonDialog;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Mapping.Model;
using ABI.System;
using System;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectLayerDialog : CommonDialog
    {
        private bool canSelect = false;

        public SelectLayerDialog(MapLayerCollection layers, Func<MapLayerInfo, bool> filter, bool excludeSelectedLayer)
        {
            InitializeComponent();
            var list = layers.Cast<MapLayerInfo>();
            list = list.Where(p => filter(p));

            if (excludeSelectedLayer)
            {
                list = list.Where(p => p != layers.Selected);
            }

            if (list.Count() > 0)
            {
                lbx.ItemsSource = list.ToList();
                lbx.SelectedIndex = 0;
                canSelect = true;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
            }
        }

        public MapLayerInfo SelectedLayer { get; set; }

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