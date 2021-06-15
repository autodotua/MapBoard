using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.WPF.Dialog;
using MapBoard.Common;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Bar;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
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
using MapBoard.Main.UI.Map.Model;
using ModernWpf.FzExtension;

namespace MapBoard.Main.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowBase, IDoAsync
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
        private bool changingSelection = false;
        private bool closing = false;
        private LayerListPanelHelper layerListHelper;
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
        /// 显示处理中遮罩并处理需要长时间运行的方法
        /// </summary>
        /// <param name="action"></param>
        /// <param name="catchException"></param>
        /// <returns></returns>
        public Task DoAsync(Func<Task> action, string message, bool catchException = false)
        {
            return DoAsync(async p => await action(), message, catchException);
        }

        /// <summary>
        /// 显示处理中遮罩并处理需要长时间运行的方法
        /// </summary>
        /// <param name="action"></param>
        /// <param name="catchException"></param>
        /// <returns></returns>
        public async Task DoAsync(Func<ProgressRingOverlayArgs, Task> action, string message, bool catchException = false)
        {
            loading.Message = message;
            loading.Show();
            try
            {
                await action(loading.TaskArgs);
            }
            catch (Exception ex)
            {
                if (catchException)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                loading.Hide();
            }
        }

        /// <summary>
        /// 初始化各组件
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            //加载地图
            await arcMap.LoadAsync();
            layerSettings.MapView = arcMap;
            //初始化各操作工具条
            foreach (var bar in new BarBase[] { editBar, selectBar, measureBar, attributesBar })
            {
                bar.MapView = arcMap;
                bar.Initialize();
            }
            //设置图层列表的数据源并初始化选中的图层
            dataGrid.ItemsSource = arcMap.Layers;
            dataGrid.SelectedItem = arcMap.Layers.Selected;
            //注册事件
            RegistEvents();
            //初始化图层列表相关操作
            layerListHelper = new LayerListPanelHelper(dataGrid, this, arcMap);
            //初始化控件可用性
            JudgeControlsEnable();
        }

        /// <summary>
        /// 为需要延迟初始化的事件进行注册
        /// </summary>
        private void RegistEvents()
        {
            arcMap.Layers.PropertyChanged += (p1, p2) =>
            {
                if (p2.PropertyName == nameof(arcMap.Layers.Selected) && !changingSelection)
                {
                    dataGrid.SelectedItem = arcMap.Layers.Selected;
                }
            };
            arcMap.Selection.CollectionChanged += (p1, p2) => JudgeControlsEnable();
            arcMap.BoardTaskChanged += (s, e) => JudgeControlsEnable();
            arcMap.Layers.LayerVisibilityChanged += (s, e) => JudgeControlsEnable();

            arcMap.Layers.PropertyChanged += (p1, p2) =>
            {
                if (p2.PropertyName == nameof(arcMap.Layers.Selected) && !changingSelection)
                {
                    dataGrid.SelectedItem = arcMap.Layers.Selected;
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
            }
            else
            {
                btnTitleBarMore.IsEnabled = true;
                if (arcMap.Selection.SelectedFeatures.Count > 0)
                {
                    dataGrid.IsEnabled = false;
                }
                else
                {
                    dataGrid.IsEnabled = true;
                }
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
                await Package.BackupAsync(arcMap.Layers, Config.MaxBackupCount);
            }
            closing = true;
            Close();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            loading.Message = "正在初始化";
            loading.Show();
        }

        private bool initialized = false;

        protected async override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (initialized)
            {
                return;
            }
            initialized = true;
            try
            {
                await DoAsync(InitializeAsync, "正在初始化");
            }
            catch (Exception ex)
            {
                CommonDialog.ShowErrorDialogAsync(ex, "初始化失败");
            }
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
            new SettingDialog(arcMap) { Owner = this }.ShowDialog();
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
            new GpxToolbox.UI.MainWindow().Show();
        }

        /// <summary>
        /// 单击瓦片地图下载器按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TileMenu_Click(object sender, RoutedEventArgs e)
        {
            new TileDownloaderSplicer.UI.MainWindow().Show();
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
            dataGrid.SelectedItem = null;
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
                 await IOUtility.ExportMapAsync(path, arcMap, arcMap.Layers, type);
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
                     await IOUtility.ImportMapAsync(path, arcMap.Layers, type, p);
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
        private void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Parameters.DataPath))
            {
                IOUtility.OpenFileOrFolder(Parameters.DataPath);
            }
            else
            {
                IOUtility.OpenFileOrFolder(FzLib.Program.App.ProgramDirectoryPath);
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
            }
            else
            {
                ani = new DoubleAnimation(grdLeft.ActualWidth, 0, TimeSpan.FromSeconds(0.5)); // { EasingFunction = EasingMode.EaseInOut };
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

        /// <summary>
        /// 图层项右键，用于显示菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            layerListHelper.ShowContextMenu();
        }

        /// <summary>
        /// 图层列表右键按下时，就使列表项被选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lvw_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            layerListHelper.RightButtonClickToSelect(e);
        }

        /// <summary>
        /// 选中的图层变化事件。图层列表选中项不使用绑定。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedLayerChanged(object sender, SelectionChangedEventArgs e)
        {
            changingSelection = true;
            if (dataGrid.SelectedItems.Count == 1)
            {
                arcMap.Layers.Selected = dataGrid.SelectedItem as MapLayerInfo;
            }
            else
            {
                arcMap.Layers.Selected = null;
            }
            changingSelection = false;
            JudgeControlsEnable();
            layerSettings.ResetLayerSettingUI();
        }

        #endregion 图层列表事件
    }
}