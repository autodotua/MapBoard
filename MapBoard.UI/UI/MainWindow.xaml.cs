﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.WPF.Dialog;
using MapBoard.IO;
using MapBoard.Model;
using MapBoard.UI.Bar;
using MapBoard.UI.Dialog;
using MapBoard.Mapping;
using ModernWpf.Controls;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ModernWpf.FzExtension;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using MapBoard.UI.TileDownloader;
using System.Threading;
using MapBoard.UI.Component;
using System.Windows.Media;
using Esri.ArcGISRuntime.Ogc;
using FzLib.Collection;
using FzLib;
using FzLib.WPF;
using System.Windows.Threading;
using WinRT;

namespace MapBoard.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MainWindowBase
    {
        #region 属性和字段

        private static readonly TwoWayDictionary<string, SketchCreationMode> ButtonsMode = new TwoWayDictionary<string, SketchCreationMode>()
        {
            { "多段线",SketchCreationMode.Polyline},
            { "自由线", SketchCreationMode.FreehandLine},
            { "多边形",SketchCreationMode.Polygon},
            { "自由面",SketchCreationMode.FreehandPolygon},
            { "圆",SketchCreationMode.Circle},
            { "椭圆",SketchCreationMode.Ellipse},
            { "箭头",SketchCreationMode.Arrow},
            { "矩形", SketchCreationMode.Rectangle},
            { "三角形",SketchCreationMode.Triangle},
            { "点",SketchCreationMode.Point},
            { "多点",SketchCreationMode.Multipoint},
        };

        private bool canClosing = true;
        private bool closing = false;
        public Config Config => Config.Instance;
        public string LoadFile { get; set; } = null;

        #endregion 属性和字段

        #region 基本方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            Exception extensionEx = null;
            try
            {
                ExtensionUtility.LoadExtensions();
            }
            catch (Exception ex)
            {
                App.Log.Error("加载参数失败", ex);

                extensionEx = ex;
            }
            InitializeComponent();

            mapInfo.Initialize(arcMap);
            if (extensionEx != null)
            {
                CommonDialog.ShowErrorDialogAsync(extensionEx, "加载扩展插件失败");
            }
        }

        /// <summary>
        /// 初始化各组件
        /// </summary>
        /// <returns></returns>
        protected override async Task InitializeAsync()
        {
            arcMap.BoardTaskChanged += (s, e) => this.Notify(nameof(IsReady));

            //加载地图
            await arcMap.LoadAsync();
            layerSettings.Initialize(arcMap);
            layersPanel.Initialize(arcMap);
            //初始化各操作工具条
            foreach (var bar in grdCenter.Children.OfType<BarBase>())
            {
                bar.MapView = arcMap;
                bar.Initialize();
            }

            //注册事件
            arcMap.Selection.CollectionChanged += (p1, p2) => ResetDrawAndSelectButton();
            arcMap.Layers.LayerPropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(LayerInfo.LayerVisible) when e.Layer == arcMap.Layers.Selected:
                    case nameof(MapLayerInfo.GeometryType) when e.Layer == arcMap.Layers.Selected:
                        ResetDrawAndSelectButton();
                        break;

                    default:
                        break;
                }
            };
            arcMap.Layers.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MapLayerCollection.Selected))
                {
                    ResetDrawAndSelectButton();
                    layerSettings.ResetLayerSettingUI();
                }
            };
            ResetDrawAndSelectButton();
            ShowLoadErrorsAsync();//不可await，因为要让主窗口显示出来
            if (LoadFile != null)
            {
                LoadMbmpkg(LoadFile);
            }
        }

        private async Task ShowLoadErrorsAsync()
        {
            if (Config.LoadError != null)
            {
                await CommonDialog.ShowErrorDialogAsync(Config.LoadError, "配置文件加载错误，将使用默认配置");
            }
            ItemsOperationErrorCollection errors;
            if ((errors = arcMap.BaseMapLoadErrors) != null)
            {
                await ItemsOperaionErrorsDialog.TryShowErrorsAsync("部分底图加载失败", errors);
            }
            if ((errors = arcMap.Layers.GetLoadErrors()) != null)
            {
                await ItemsOperaionErrorsDialog.TryShowErrorsAsync("部分图层加载失败", errors);
            }
        }

        public bool IsReady => arcMap != null && arcMap.CurrentTask == BoardTask.Ready;

        /// <summary>
        /// 重置绘制和选择按钮的可见性
        /// </summary>
        private void ResetDrawAndSelectButton()
        {
            if (!CheckAccess())
            {
                Dispatcher.Invoke(ResetDrawAndSelectButton);
                return;
            }
            this.Notify(nameof(IsReady));
            grdButtons.Children.OfType<SplitButton>()
                   .ForEach(p => p.Visibility = Visibility.Collapsed);
            if (arcMap.Layers.Selected != null && arcMap.Layers.Selected is IEditableLayerInfo)
            {
                UIElement btn = arcMap.Layers.Selected.GeometryType switch
                {
                    GeometryType.Multipoint => splBtnMultiPoint,
                    GeometryType.Point => splBtnPoint,
                    GeometryType.Polyline => splBtnPolyline,
                    GeometryType.Polygon => splBtnPolygon,
                    _ => null
                };
                if (btn != null)
                {
                    btn.Visibility = Visibility.Visible;
                }
            }
            else
            {
                btnDraw.Visibility = Visibility.Visible;
            }
        }

        public void Close(bool force)
        {
            if (force)
            {
                closing = true;
            }
            Close();
        }

        private async void WindowClosing(object sender, CancelEventArgs e)
        {
            if (closing)
            {
                return;
            }
            e.Cancel = true;
            if (!canClosing)
            {
                return;
            }
            Config.Save();
            if (arcMap.Layers != null)
            {
                arcMap.Layers.Save();
            }
            if (arcMap.CurrentTask == BoardTask.Draw)
            {
                await CommonDialog.ShowErrorDialogAsync("请先结束绘制");
                return;
            }
            Hide();
            if (Config.BackupWhenExit)
            {
                try
                {
                    await Package.BackupAsync(arcMap.Layers, Config.MaxBackupCount, Config.CopyShpFileWhenExport);
                }
                catch (Exception ex)
                {
                    App.Log.Error(ex);
                }
            }
            closing = true;
            Close();
        }

        /// <summary>
        /// 在地图上托放如文件的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ArcMap_PreviewDrop(object sender, DragEventArgs e)
        {
            BringToFront();
            if (arcMap.CurrentTask != BoardTask.Ready)
            {
                return;
            }
            if (e.Data.GetDataPresent(typeof(MapLayerInfo).FullName))
            {
                return;
            }
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files) || files.Length == 0)
            {
                SnakeBar.ShowError(this, "不支持的格式");
                return;
            }
            if (files.Select(p => Path.GetExtension(p)).Distinct().Count() > 1)
            {
                SnakeBar.ShowError(this, "不支持拖放含多个格式的文件或文件夹");
            }
            int fileCount = files.Count(p => File.Exists(p));
            int folderCount = files.Count(p => Directory.Exists(p));
            if (fileCount + folderCount == 0)
            {
                return;
            }
            if (fileCount * folderCount != 0)
            {
                SnakeBar.ShowError(this, "不支持同时包含文件和文件夹");
            }
            await DoAsync(async () =>
            {
                if (fileCount > 0)
                {
                    await IOUtility.DropFilesAsync(files, arcMap.Layers);
                }
                else
                {
                    await IOUtility.DropFoldersAsync(files, arcMap.Layers);
                }
            }, "正在导入");
        }

        /// <summary>
        /// 鼠标移动事件，主要用于更新右下角的位置和比例尺信息
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        /// <summary>
        /// 地图视角变化事件，用于更新右下角的位置和比例尺信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArcMap_ViewpointChanged(object sender, EventArgs e)
        {
        }

        private void WindowBase_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height < 800)
            {
                layerSettings.Height = 240;
            }
            else if (e.NewSize.Height < 1050)
            {
                layerSettings.Height = 360;
            }
            else
            {
                layerSettings.Height = 480;
            }
        }

        #endregion 基本方法

        #region 菜单栏操作

        /// <summary>
        /// 单击设置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingDialog(this, arcMap.Layers).ShowDialog();
        }

        /// <summary>
        /// 单击关于按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            CommonDialog.ShowOkDialogAsync("关于", "开发人员：autodotua", "github:https://github.com/autodotua/MapBoard");
        }

        /// <summary>
        /// 单击清除图层历史操作按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearHistoriesButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var layer in arcMap.Layers.OfType<IEditableLayerInfo>())
            {
                layer.Histories.Clear();
            }
        }

        /// <summary>
        /// 单击测量面积和周长按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeasureAreaMenuItem_Click(object sender, RoutedEventArgs e)
        {
            arcMap.Editor.MeasureArea();
        }

        /// <summary>
        /// 单击测量长度按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MeasureLengthMenuItem_Click(object sender, RoutedEventArgs e)
        {
            arcMap.Editor.MeasureLength();
        }

        /// <summary>
        /// 单击GPX工具箱按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GpxMenu_Click(object sender, RoutedEventArgs e)
        {
            new GpxToolbox.GpxWindow().Show();
        }

        /// <summary>
        /// 单击瓦片地图下载器按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TileMenu_Click(object sender, RoutedEventArgs e)
        {
            new TileDownloaderWindow().Show();
        }

        /// <summary>
        /// 打开场景浏览窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BrowseSceneMenu_Click(object sender, RoutedEventArgs e)
        {
            var win = new BrowseSceneWindow();
            await DoAsync(win.LoadAsync, "正在加载地图");
            win.Show();
        }

        #endregion 菜单栏操作

        #region 样式设置区事件

        /// <summary>
        /// 单击新建图层按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddWfsLayerButtonClick(object sender, RoutedEventArgs e)
        {
            await new AddWfsLayerDialog(arcMap.Layers).ShowAsync();
            arcMap.Layers.Save();
        }

        private async void CreateTempLayerButtonClick(object sender, RoutedEventArgs e)
        {
            await CreateLayerDialog.OpenCreateDialog<TempMapLayerInfo>(arcMap.Layers);
            arcMap.Layers.Save();
        }

        private async void CreateLayerButtonClick(SplitButton sender, SplitButtonClickEventArgs args)
        {
            await CreateLayerDialog.OpenCreateDialog<ShapefileMapLayerInfo>(arcMap.Layers);
            arcMap.Layers.Save();
        }

        /// <summary>
        /// 单击应用样式按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ApplyStyleButtonClick(object sender, RoutedEventArgs e)
        {
            await layerSettings.SetStyleFromUI();
            arcMap.Layers.Save();
        }

        /// <summary>
        /// 单击浏览模式按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowseModeButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.Layers.Selected = null;
        }

        #endregion 样式设置区事件

        #region 导入导出区事件

        /// <summary>
        ///单击导出菜单项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExportMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Parameters.DataPath) || !Directory.EnumerateFiles(Parameters.DataPath).Any())
            {
                SnakeBar.ShowError("没有任何数据");
                return;
            }
            canClosing = false;
            ExportMapType type = (ExportMapType)int.Parse((sender as FrameworkElement).Tag as string);
            string path = null;
            if (type is not ExportMapType.GISToolBoxNet)
            {
                IOUtility.GetExportMapPath(type, this);
            }
            else
            {
                path = "";
            }
            if (path != null)
            {
                await DoAsync(async p =>
                 {
                     switch (type)
                     {
                         case ExportMapType.GISToolBoxNet:
                             try
                             {
                                 string ip = await CommonDialog.ShowInputDialogAsync("请输入GIS工具箱的IP地址", Config.Instance.LastFTP);
                                 if (ip != null)
                                 {
                                     ip = ip.Trim();
                                     if (ip.StartsWith("ftp://"))
                                     {
                                         ip = ip["ftp://".Length..];
                                     }
                                     ip = ip.Trim('/').Trim('\\').Replace("：", ":");
                                     Config.Instance.LastFTP = ip;
                                     string shpDir = "Shapefile";
                                     using (FTPHelper ftp = new FTPHelper(ip, "", "", ""))
                                     {
                                         p.SetMessage("正在连接FTP");
                                         if (!await ftp.IsDirectoryExistAsync(shpDir))
                                         {
                                             await ftp.CreateDirectoryAsync(shpDir);
                                         }
                                         ftp.GotoDirectory(shpDir, false);

                                         p.SetMessage("正在准备文件");
                                         string dir = await MobileGISToolBox.ExportMathToTempDirAsync(arcMap.Layers);
                                         foreach (var file in Directory.EnumerateFiles(Path.Combine(dir, shpDir)))
                                         {
                                             p.SetMessage("正在上传：" + Path.GetFileName(file));
                                             await ftp.UploadAsync(file);
                                         }
                                     }
                                     SnakeBar.Show(this, "上传到FTP完成");
                                 }

                                 //CancellationTokenSource cts = new CancellationTokenSource();
                                 ////Task.Delay(5000).ContinueWith(t => cts.Cancel());
                                 //string[] ips = HttpServerUtil.GetIPs();
                                 //var result = await CommonDialog.ShowSelectItemDialogAsync("选择本地IP", ips.Select(p => new SelectDialogItem(p)));
                                 //if (result >= 0)
                                 //{
                                 //    await MobileGISToolBox.OpenHttpServerAsync(ips[result], arcMap.Layers, cts.Token);
                                 //}
                             }
                             catch (Exception ex)
                             {
                                 App.Log.Error("上传失败", ex);
                                 await CommonDialog.ShowErrorDialogAsync(ex, "上传失败");
                             }
                             break;

                         case ExportMapType.OpenLayers:
                             try
                             {
                                 var visiableOnly = arcMap.Layers.Any(p => p.LayerVisible) && await CommonDialog.ShowYesNoDialogAsync("是否仅导出可见图层？");

                                 await new OpenLayers(path, Directory.GetFiles("res/openlayers"),
                                    Config.Instance.BaseLayers.ToArray(),
                                    arcMap.Layers.OfType<IMapLayerInfo>().Where(p => visiableOnly ? p.LayerVisible : true).ToArray())
                                 .ExportAsync();
                                 IOUtility.ShowExportedSnackbarAndClickToOpenFolder(path, this);
                             }
                             catch (Exception ex)
                             {
                                 App.Log.Error("导出失败", ex);
                                 await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
                             }
                             break;

                         default:
                             await IOUtility.ExportMapAsync(this, path, arcMap, arcMap.Layers, type);
                             break;
                     }
                 }, "正在导出");
            }

            canClosing = true;
        }

        /// <summary>
        /// 单击导出按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportButton_Click(object sender, SplitButtonClickEventArgs e)
        {
            ExportMenu_Click(sender, (RoutedEventArgs)null);
        }

        /// <summary>
        /// 单击导入菜单项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ImportMenu_Click(object sender, RoutedEventArgs e)
        {
            var type = (ImportMapType)int.Parse((sender as FrameworkElement).Tag as string);
            string path = IOUtility.GetImportMapPath(type, this);
            if (path != null)
            {
                await DoAsync(async p =>
                 {
                     await IOUtility.ImportMapAsync(this, path, arcMap.Layers, type, p);
                 }, "正在导入");
            };
        }

        /// <summary>
        /// 单击导入按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ImportButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            ImportMenu_Click(sender, (RoutedEventArgs)null);
        }

        /// <summary>
        /// 单击“目录”按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            string path = ((sender as FrameworkElement).Tag as string) switch
            {
                "1" => Parameters.DataPath,
                "2" => FzLib.Program.App.ProgramDirectoryPath,
                "3" => Path.GetDirectoryName(Parameters.ConfigPath),
                "4" => Parameters.BackupPath,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (Directory.Exists(path))
            {
                IOUtility.OpenFileOrFolder(path);
            }
            else
            {
                await CommonDialog.ShowErrorDialogAsync("目录不存在");
            }
        }

        private void OpenFolderButtonClick(SplitButton sender, SplitButtonClickEventArgs args)
        {
            OpenFolderButtonClick(sender, (RoutedEventArgs)null);
        }

        public async void LoadMbmpkg(string path)
        {
            if (!File.Exists(path))
            {
                await CommonDialog.ShowErrorDialogAsync("找不到文件" + path);
                return;
            }
            if (await CommonDialog.ShowYesNoDialogAsync("加载地图包", "是否加载地图包？这将会覆盖当前的所有图层"))
            {
                await DoAsync(async p =>
                {
                    await IOUtility.ImportMapAsync(this, path, arcMap.Layers, ImportMapType.MapPackageOverwrite, p);
                }, "正在导入");
            }
        }

        #endregion 导入导出区事件

        #region 绘制区域事件

        /// <summary>
        /// 单击绘制按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void DrawButtonsClick(SplitButton sender, SplitButtonClickEventArgs args)
        {
            await StartDraw(sender);
        }

        /// <summary>
        /// 单击绘制按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DrawButtonsClick(object sender, RoutedEventArgs e)
        {
            await StartDraw(sender);
        }

        /// <summary>
        /// 单击选择按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SelectToggleButtonClick(object sender, RoutedEventArgs e)
        {
            await arcMap.Selection.SelectRectangleAsync();
        }

        /// <summary>
        /// 单击绘制按钮事件，根据按钮绘制指定类型
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private async Task StartDraw(object sender)
        {
            string text;
            if (sender is SplitButton)
            {
                text = (sender as SplitButton).Content as string;
            }
            else
            {
                text = (sender as MenuItem).Header as string;
            }
            if (arcMap.CurrentTask == BoardTask.Select)
            {
                arcMap.Selection.ClearSelection();
            }

            var mode = ButtonsMode[text];
            await arcMap.Editor.DrawAsync(mode);
        }

        #endregion 绘制区域事件

        #region 图层列表事件

        /// <summary>
        /// 单击左侧图层设置区域的长条
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayerSettingOpenCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            btn.IsEnabled = false;
            DoubleAnimation ani;
            if (grdLeft.ActualWidth == 0)
            {
                ani = new DoubleAnimation(0, 300, TimeSpan.FromSeconds(0.5));
                grdLeftArea.SetResourceReference(BackgroundProperty, "SystemControlPageBackgroundAltHighBrush");
                grdCenter.Margin = new Thickness(0);
                (btnShrink.LayoutTransform as ScaleTransform).ScaleX = 0.5;
            }
            else
            {
                ani = new DoubleAnimation(grdLeft.ActualWidth, 0, TimeSpan.FromSeconds(0.5)).SetInOutCubicEase(); // { EasingFunction = EasingMode.EaseInOut };

                (btnShrink.LayoutTransform as ScaleTransform).ScaleX = -0.5;
            }
            ani.Completed += (p1, p2) =>
            {
                btn.IsEnabled = true;
                if (((p1 as AnimationClock).Timeline as DoubleAnimation).To == 0)
                {
                    grdLeftArea.Background = Brushes.Transparent;
                    grdCenter.Margin = new Thickness(-30, 0, 0, 0);
                }
            };

            grdLeft.BeginAnimation(WidthProperty, ani);
        }

        #endregion 图层列表事件
    }
}