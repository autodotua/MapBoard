using FzLib.Control.Extension;
using MapBoard.Common;
using MapBoard.Main.UI.Map;
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

        public Config Config => MapBoard.Common.Config.Instance;

        private async void ApplyUrlButtonClick(object sender, RoutedEventArgs e)
        {
            await ArcMapView.Instance.LoadBasemap();
        }

        private async void ResetUrlButtonClick(object sender, RoutedEventArgs e)
        {
            Config.Url = "http://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&scl=1&style=8&x={x}&y={y}&z={z}";
            Notify(nameof(Config));
            await ArcMapView.Instance.LoadBasemap();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var win = new MapBoard.GpxToolbox.MainWindow();
            win.Show();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            new MapBoard.TileDownloaderSplicer.MainWindow().Show();
        }
    }
}
