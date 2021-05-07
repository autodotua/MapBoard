using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.Common.Dialog;
using MapBoard.Main.Model;
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
using System.Windows.Shapes;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SettingDialog : Common.Dialog.DialogWindowBase
    {
        private bool canSelect = false;

        public SettingDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public Config Config => Config.Instance;

        private void SetBaseLayersButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialog.BaseLayerSettingDialog();
            dialog.Closed += (p1, p2) =>
            {
                Notify(nameof(Config));
            };
            dialog.ShowDialog();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (ArcMapView.Instance != null)
            {
                ArcMapView.Instance.SetHideWatermark();
            }
            foreach (var map in GpxToolbox.ArcMapView.Instances)
            {
                map.SetHideWatermark();
            }
        }

        private void DialogWindowBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.Instance.Save();
        }
    }
}