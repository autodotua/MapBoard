using FzLib.UI.Extension;
using MapBoard.Common;
using MapBoard.Main.UI.Map;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapBoard.Main.UI.Panel
{
    /// <summary>
    /// SettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPanel : ExtendedUserControl
    {
        public SettingPanel()
        {
            InitializeComponent();
        }

        public Config Config => Config.Instance;

        private async void ApplyUrlButtonClick(object sender, RoutedEventArgs e)
        {
            await ArcMapView.Instance.LoadBasemap();
        }

        private async void ResetUrlButtonClick(object sender, RoutedEventArgs e)
        {
            //Config.Url = "http://t0.tianditu.com/vec_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=vec&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=9396357d4b92e8e197eafa646c3c541d\r\nhttp://t0.tianditu.com/cva_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=cva&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=9396357d4b92e8e197eafa646c3c541d";
            Notify(nameof(Config));
            await ArcMapView.Instance.LoadBasemap();
        }

        private void GpxToolboxButtonClick(object sender, RoutedEventArgs e)
        {
            var win = new GpxToolbox.MainWindow();
            win.Show();
        }

        private void TileDownloaderSplicerButtonClick(object sender, RoutedEventArgs e)
        {
            new TileDownloaderSplicer.MainWindow().Show();
        }

        private void GpxHeightCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            //stkGpxHeight.Visibility = (sender as CheckBox).IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetBaseLayersButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialog.BaseLayerSettingDialog();
            dialog.Closed += (p1, p2) =>
            {
                Notify(nameof(Config));
            };
            dialog.Show();
        }
    }
}