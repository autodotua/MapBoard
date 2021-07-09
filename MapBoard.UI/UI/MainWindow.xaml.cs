using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
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
using static FzLib.Basic.Loop;
using ModernWpf.FzExtension;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using MapBoard.UI.TileDownloader;
using System.Threading;
using MapBoard.UI.Component;
using System.Windows.Media;

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
            RegistEvents();
            //初始化控件可用性
            JudgeControlsEnable();
            if (arcMap.Layers.LoadErrors != null)
            {
                ItemsOperaionErrorsDialog.TryShowErrorsAsync("部分图层加载失败", arcMap.Layers.LoadErrors);
            }
        }

        /// <summary>
        /// 为需要延迟初始化的事件进行注册
        /// </summary>
        private void RegistEvents()
        {
            arcMap.Selection.CollectionChanged += (p1, p2) => JudgeControlsEnable();
            arcMap.BoardTaskChanged += (s, e) => JudgeControlsEnable();
            arcMap.Layers.LayerPropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LayerInfo.LayerVisible) && e.Layer == arcMap.Layers.Selected)
                {
                    JudgeControlsEnable();
                }
            };
        }

        /// <summary>
        /// 在某些状态发生变更之后，重新判断各控件是否可用
        /// </summary>
        private void JudgeControlsEnable()
        {
            if (arcMap.CurrentTask == BoardTask.Draw
                || arcMap.CurrentTask == BoardTask.Select)
            {
                grdLeft.IsEnabled = false;
                btnTitleBarMore.IsEnabled = false;
                mapInfo.IsEnabled = false;
            }
            else
            {
                mapInfo.IsEnabled = true;
                btnTitleBarMore.IsEnabled = true;
                layersPanel.JudgeControlsEnable();
                grdLeft.IsEnabled = true;
            }
            btnApplyStyle.IsEnabled = btnBrowseMode.IsEnabled = layerSettings.IsEnabled = arcMap.Layers.Selected != null;

            if (IsLoaded)
            {
                btnSelect.IsEnabled = arcMap.Layers.Selected != null;
                grdButtons.IsEnabled = arcMap.Layers.Selected == null || arcMap.Layers.Selected.LayerVisible;

                var buttons = grdButtons.Children.OfType<SplitButton>();
                buttons.ForEach(p => p.Visibility = Visibility.Collapsed);
                if (arcMap.Layers.Selected != null)
                {
                    switch (arcMap.Layers.Selected.GeometryType)
                    {
                        case GeometryType.Multipoint:
                            splBtnMultiPoint.Visibility = Visibility.Visible;
                            arcMap.Editor.CurrentDrawMode = SketchCreationMode.Multipoint;
                            break;

                        case GeometryType.Point:
                            splBtnPoint.Visibility = Visibility.Visible;
                            arcMap.Editor.CurrentDrawMode = SketchCreationMode.Point;
                            break;

                        case GeometryType.Polyline:
                            splBtnPolyline.Visibility = Visibility.Visible;
                            arcMap.Editor.CurrentDrawMode = SketchCreationMode.Polyline;
                            break;

                        case GeometryType.Polygon:
                            splBtnPolygon.Visibility = Visibility.Visible;
                            arcMap.Editor.CurrentDrawMode = SketchCreationMode.Polygon;
                            break;
                    }
                }
                else
                {
                    arcMap.Editor.CurrentDrawMode = null;
                }
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
            foreach (MapLayerInfo layer in arcMap.Layers)
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
        private async void CreateLayerButtonClick(object sender, RoutedEventArgs e)
        {
            await new CreateLayerDialog(arcMap.Layers).ShowAsync();
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
            canClosing = false; ExportMapType type = (ExportMapType)int.Parse((sender as FrameworkElement).Tag as string);
            string path = IOUtility.GetExportMapPath(type);
            if (path != null)
            {
                await DoAsync(async () =>
             {
                 await IOUtility.ExportMapAsync(this, path, arcMap, arcMap.Layers, type);
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
            string path = IOUtility.GetImportMapPath(type);
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
            arcMap.Editor.CurrentDrawMode = mode;
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
                ani = new DoubleAnimation(grdLeft.ActualWidth, 0, TimeSpan.FromSeconds(0.5)); // { EasingFunction = EasingMode.EaseInOut };

                (btnShrink.LayoutTransform as ScaleTransform).ScaleX = -0.5;
            }
            ani.Completed += (p1, p2) =>
            {
                btn.IsEnabled = true;
                if (((p1 as AnimationClock).Timeline as DoubleAnimation).To == 0)
                {
                    grdLeftArea.Background = System.Windows.Media.Brushes.Transparent;
                    grdCenter.Margin = new Thickness(-30, 0, 0, 0);
                }
            };
            ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };

            grdLeft.BeginAnimation(WidthProperty, ani);
        }

        #endregion 图层列表事件

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

        private void layersPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            JudgeControlsEnable();
            layerSettings.ResetLayerSettingUI();
        }
    }
}