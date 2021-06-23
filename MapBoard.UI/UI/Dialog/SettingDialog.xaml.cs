﻿using FzLib.Extension;
using FzLib.WPF.Dialog;
using FzLib.WPF.Extension;
using MapBoard.Model;
using MapBoard.Mapping;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SettingDialog : DialogWindowBase
    {
        public SettingDialog(Window owner, ArcMapView mapView) : base(owner)
        {
            MapView = mapView;
            BaseLayers = new ObservableCollection<BaseLayerInfo>(
              Config.Instance.BaseLayers.Select(p => p.Clone()));
            ResetIndex();
            BaseLayers.CollectionChanged += (p1, p2) => ResetIndex();
            InitializeComponent();
            cbbCoords.ItemsSource = Enum.GetValues(typeof(CoordinateSystem)).Cast<CoordinateSystem>();
            new DataGridHelper<BaseLayerInfo>(grd).EnableDragAndDropItem();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string appPath = FzLib.Program.App.ProgramDirectoryPath;
            if (File.Exists(Path.Combine(appPath, Parameters.ConfigHere)))
            {
                rbtnHere.IsChecked = true;
            }
            else if (File.Exists(Path.Combine(appPath, Parameters.ConfigUp)))
            {
                rbtnUp.IsChecked = true;
            }
            else
            {
                rbtnAppData.IsChecked = true;
            }
        }

        public Config Config => Config.Instance;

        public int Theme
        {
            get => Config.Theme switch
            {
                0 => 0,
                1 => 1,
                -1 => 2,
                _ => throw new ArgumentOutOfRangeException()
            };
            set => Config.Theme = value switch
            {
                0 => 0,
                1 => 1,
                2 => -1,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            foreach (var map in ArcMapView.Instances)
            {
                map.SetHideWatermark();
            }
            foreach (var map in GpxMapView.Instances)
            {
                map.SetHideWatermark();
            }
            foreach (var map in TileDownloaderMapView.Instances)
            {
                map.SetHideWatermark();
            }
        }

        private void DialogWindowBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.Instance.Save();
        }

        public ObservableCollection<BaseLayerInfo> BaseLayers { get; }
        public IEnumerable<BaseLayerType> BaseLayerTypes { get; } = Enum.GetValues(typeof(BaseLayerType)).Cast<BaseLayerType>().ToList();
        public ArcMapView MapView { get; }

        private void ResetIndex()
        {
            for (int i = 0; i < BaseLayers.Count; i++)
            {
                BaseLayers[i].Index = i + 1;
            }
        }

        private async void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Config.Instance.BaseLayers = BaseLayers.ToList();
            Config.Instance.Save();
            App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            (Owner as MainWindow).Close(true);
            foreach (Window window in App.Current.Windows.Cast<Window>().ToList())
            {
                window.Close();
            }
            App.Current.MainWindow = new MainWindow();
            App.Current.MainWindow.Show();
            App.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            Close();
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            BaseLayerInfo layerInfo = new BaseLayerInfo(BaseLayerType.WebTiledLayer, "");
            BaseLayers.Add(layerInfo);
            grd.SelectedItem = layerInfo;
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e)
        {
            string path = FileSystemDialog.GetOpenFile(new FileFilterCollection()
                 .Add("JPEG图片", "jpg,jpeg")
                 .Add("PNG图片", "png")
                 .Add("BMP图片", "bmp")
                 .Add("TIFF图片", "tif,tiff")
                 .Add("Shapefile矢量图", "shp")
                 .Add("TilePackage切片包", "tpk")
                 .AddUnion());
            if (path == null)
            {
                return;
            }

            var layerInfo = System.IO.Path.GetExtension(path) switch
            {
                ".shp" => new BaseLayerInfo(BaseLayerType.ShapefileLayer, path),
                ".tpk" => new BaseLayerInfo(BaseLayerType.TpkLayer, path),
                _ => new BaseLayerInfo(BaseLayerType.RasterLayer, path),
            };
            BaseLayers.Add(layerInfo);
            grd.SelectedItem = layerInfo;
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (grd.SelectedItem != null)
            {
                BaseLayers.Remove(grd.SelectedItem as BaseLayerInfo);
            }
        }

        private async void RbtnDataPath_Click(object sender, RoutedEventArgs e)
        {
            string appPath = FzLib.Program.App.ProgramDirectoryPath;
            try
            {
                if (File.Exists(Path.Combine(appPath, Parameters.ConfigHere)))
                {
                    File.Delete(Path.Combine(appPath, Parameters.ConfigHere));
                }
                if (File.Exists(Path.Combine(appPath, Parameters.ConfigUp)))
                {
                    File.Delete(Path.Combine(appPath, Parameters.ConfigUp));
                }
                if (rbtnHere.IsChecked.Value)
                {
                    File.WriteAllText(Path.Combine(appPath, Parameters.ConfigHere), "");
                }
                else if (rbtnUp.IsChecked.Value)
                {
                    File.WriteAllText(Path.Combine(appPath, Parameters.ConfigUp), "");
                }
                await CommonDialog.ShowOkDialogAsync("修改数据位置", "将在重启后生效");
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "修改数据位置失败");
            }
        }
    }
}