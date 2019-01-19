using FzLib.Control.Extension;
using MapBoard.UI.Map;
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

namespace MapBoard.UI.Panel
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
            Config.Url= "http://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&scl=1&style=8&x={x}&y={y}&z={z}";
            Notify(nameof(Config));
            await ArcMapView.Instance.LoadBasemap();
        }
    }
}
