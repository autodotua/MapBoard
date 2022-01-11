﻿using FzLib;
using FzLib.WPF.Dialog;
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
using MapBoard.IO;
using MapBoard.Mapping.Model;
using Microsoft.WindowsAPICodePack.FzExtension;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModernWpf.Controls;
using FzLib.DataStorage.Serialization;
using MapBoard.Util;
using System.ComponentModel;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SettingDialog : DialogWindowBase
    {
        public SettingDialog(Window owner, MapLayerCollection layers, int tabIndex = 0) : base(owner)
        {
            FormatMbmpkgAssociated = FileFormatAssociationUtility.IsAssociated("mbmpkg", MbmpkgID);
            FormatGpxAssociated = FileFormatAssociationUtility.IsAssociated("gpx", gpxID);

            BaseLayers = new ObservableCollection<BaseLayerInfo>(
              Config.Instance.BaseLayers.Select(p => p.Clone()));
            ResetIndex();
            BaseLayers.CollectionChanged += (p1, p2) => ResetIndex();
            InitializeComponent();
            cbbCoords.ItemsSource = Enum.GetValues(typeof(CoordinateSystem)).Cast<CoordinateSystem>();
            Layers = layers;
            tab.SelectedIndex = tabIndex;
        }

        /// <summary>
        /// 窗口启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 是否设置了地图画板地图包格式关联
        /// </summary>
        public bool FormatMbmpkgAssociated { get; set; }

        /// <summary>
        /// 是否设置了GPX格式关联
        /// </summary>
        public bool FormatGpxAssociated { get; set; }

        /// <summary>
        /// 主题
        /// </summary>
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

        /// <summary>
        /// 点击水印选择框按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WatermarkCheckBox_Click(object sender, RoutedEventArgs e)
        {
            foreach (var map in MainMapView.Instances)
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

        /// <summary>
        /// 是否可以关闭窗口
        /// </summary>
        private bool canClose = true;

        /// <summary>
        /// 对话框窗口关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DialogWindowBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
                return;
            }
            Config.Instance.Save();
        }

        /// <summary>
        /// 所有底图图层
        /// </summary>
        public ObservableCollection<BaseLayerInfo> BaseLayers { get; }

        /// <summary>
        /// 底图类型
        /// </summary>
        public IEnumerable<BaseLayerType> BaseLayerTypes { get; } = Enum.GetValues(typeof(BaseLayerType)).Cast<BaseLayerType>().ToList();

        /// <summary>
        /// 所有图层
        /// </summary>
        public MapLayerCollection Layers { get; }

        /// <summary>
        /// 重新为底图序号赋值
        /// </summary>
        private void ResetIndex()
        {
            for (int i = 0; i < BaseLayers.Count; i++)
            {
                BaseLayers[i].Index = i + 1;
            }
        }

        /// <summary>
        /// 点击底图确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Config.Instance.BaseLayers = BaseLayers.ToList();

            RestartMainWindow();
        }

        /// <summary>
        /// 点击删除底图按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (grd.SelectedItem != null)
            {
                BaseLayers.Remove(grd.SelectedItem as BaseLayerInfo);
            }
        }

        /// <summary>
        /// 点击数据目录按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 点击备份按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            canClose = false;
            (sender as Button).IsEnabled = false;
            try
            {
                await Package.BackupAsync(Layers, Config.Instance.MaxBackupCount, Config.Instance.CopyShpFileWhenExport);
            }
            catch (Exception ex)
            {
                CommonDialog.ShowErrorDialogAsync(ex, "备份失败");
            }
            this.Notify(nameof(CurrentBackupCount));

            (sender as Button).IsEnabled = true;
            canClose = true;
        }

        /// <summary>
        /// 点击打开备份目录按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenBackupFolderButton_Click(object sender, RoutedEventArgs e)
        {
            IOUtility.OpenFileOrFolder(Parameters.BackupPath);
        }

        /// <summary>
        /// 当前备份数量
        /// </summary>
        public int CurrentBackupCount => Directory.EnumerateFiles(Parameters.BackupPath, "*.mbmpkg").Count();

        /// <summary>
        /// 点击新增底图图层按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AddButtonClick(SplitButton sender, SplitButtonClickEventArgs args)
        {
            BaseLayerInfo layerInfo = new BaseLayerInfo(BaseLayerType.WebTiledLayer, "");
            BaseLayers.Add(layerInfo);
            SelectAndScroll(layerInfo);
        }

        /// <summary>
        /// 点击新增WMS图层按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddWmsButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new AddWmsLayerDialog();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                BaseLayerInfo layerInfo = new BaseLayerInfo(BaseLayerType.WmsLayer, $"{dialog.Url}|{dialog.WmsLayerName}");
                BaseLayers.Add(layerInfo);
                SelectAndScroll(layerInfo);
            }
        }

        /// <summary>
        /// 点击新增文件按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFileButtonClick(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection()
                           .Add("JPEG图片", "jpg,jpeg")
                           .Add("PNG图片", "png")
                           .Add("BMP图片", "bmp")
                           .Add("TIFF图片", "tif,tiff")
                           .Add("Shapefile矢量图", "shp")
                           .Add("TilePackage切片包", "tpk")
                           .AddUnion()
                           .CreateOpenFileDialog()
                           .SetParent(this)
                           .GetFilePath();
            if (path == null)
            {
                return;
            }

            var layerInfo = Path.GetExtension(path) switch
            {
                ".shp" => new BaseLayerInfo(BaseLayerType.ShapefileLayer, path),
                ".tpk" => new BaseLayerInfo(BaseLayerType.TpkLayer, path),
                _ => new BaseLayerInfo(BaseLayerType.RasterLayer, path),
            };
            BaseLayers.Add(layerInfo);
            SelectAndScroll(layerInfo);
        }

        /// <summary>
        /// 选择并定位
        /// </summary>
        /// <param name="item"></param>
        private void SelectAndScroll(object item)
        {
            grd.SelectedItem = item;
            grd.ScrollIntoView(item);
        }

        /// <summary>
        /// 点击导出设置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().Add("MapBoard配置文件", "mbconfig")
                .CreateSaveFileDialog()
                .SetDefault("地图画板配置", null, "mbconfig")
                .SetParent(this)
                .GetFilePath();
            if (path != null)
            {
                Config.Save(path);
            }
        }

        /// <summary>
        /// 点击导入设置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection()
                .Add("MapBoard配置文件", "mbconfig")
                .CreateOpenFileDialog()
                .SetParent(this)
                .GetFilePath();
            if (path != null)
            {
                try
                {
                    Config.Instance.LoadFromJsonFile(path);
                    RestartMainWindow();
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "加载配置文件失败");
                }
            }
        }

        /// <summary>
        /// 重启主窗口
        /// </summary>
        private void RestartMainWindow()
        {
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

        /// <summary>
        /// 地图画板地图包格式的ID
        /// </summary>
        private const string MbmpkgID = "MapBoard_mpmpkg";

        /// <summary>
        /// GPX格式的ID
        /// </summary>
        private const string gpxID = "MapBoard_gpx";

        /// <summary>
        /// 点击关联格式选择框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        private void FileAssociateCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var isChecked = (sender as CheckBox).IsChecked == true;
            if (isChecked)
            {
                switch ((sender as CheckBox).Tag as string)
                {
                    case "mbmpkg":
                        FileFormatAssociationUtility.SetAssociation("mbmpkg", MbmpkgID, "地图画板地图包", Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "res", "icon.ico"));
                        break;

                    case "gpx":
                        FileFormatAssociationUtility.SetAssociation("gpx", gpxID, "GPS轨迹文件", Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "res", "icon.ico"));
                        break;

                    default:
                        throw new InvalidEnumArgumentException();
                }
            }
            else
            {
                switch ((sender as CheckBox).Tag as string)
                {
                    case "mbmpkg":
                        FileFormatAssociationUtility.DeleteAssociation("mbmpkg", MbmpkgID);
                        break;

                    case "gpx":
                        FileFormatAssociationUtility.DeleteAssociation("gpx", gpxID);
                        break;

                    default:
                        throw new InvalidEnumArgumentException();
                }
            }
        }
    }
}