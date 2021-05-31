using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.WPF.Dialog;
using MapBoard.Common;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Bar;
using MapBoard.Main.UI.Component;
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

namespace MapBoard.Main.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowBase
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
            InitializeComponent();
        }

        public async Task DoAsync(Func<Task> action, bool catchException = false)
        {
            loading.Show();
            try
            {
                await action();
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

        public async Task InitializeAsync()
        {
            await arcMap.LoadAsync();
            layerSettings.MapView = arcMap;
            foreach (var bar in new BarBase[] { editBar, selectBar, measureBar, attributesBar })
            {
                bar.MapView = arcMap;
                bar.Initialize();
            }
            dataGrid.ItemsSource = arcMap.Layers;
            dataGrid.SelectedItem = arcMap.Layers.Selected;
            arcMap.Layers.PropertyChanged += (p1, p2) =>
            {
                if (p2.PropertyName == nameof(arcMap.Layers.Selected) && !changingSelection)
                {
                    dataGrid.SelectedItem = arcMap.Layers.Selected;
                }
            };
            RegistEvents();
            layerListHelper = new LayerListPanelHelper(dataGrid, p => DoAsync(p), arcMap);
            JudgeControlsEnable();

            if (arcMap.Layers.Selected != null
                && arcMap.Layers.Selected != null
                && arcMap.Layers.Selected.NumberOfFeatures > 0)
            {
                layerSettings.ResetLayerSettingUI();
            }
        }

        private void RegistEvents()
        {
            arcMap.Selection.CollectionChanged += (p1, p2) => JudgeControlsEnable();
            arcMap.BoardTaskChanged += (s, e) => JudgeControlsEnable();
            arcMap.Layers.LayerVisibilityChanged += (s, e) => JudgeControlsEnable();

            var lvwHelper = new LayerListViewHelper(dataGrid);
            lvwHelper.EnableDragAndDropItem();

            arcMap.Layers.PropertyChanged += (p1, p2) =>
            {
                if (p2.PropertyName == nameof(arcMap.Layers.Selected) && !changingSelection)
                {
                    dataGrid.SelectedItem = arcMap.Layers.Selected;
                }
            };
        }

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
            arcMap.Layers.Save();
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

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            await InitializeAsync();
        }

        private async void arcMap_PreviewDrop(object sender, DragEventArgs e)
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
            });
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mapInfo.Update(arcMap, e.GetPosition(arcMap));
        }

        private void arcMap_ViewpointChanged(object sender, EventArgs e)
        {
            mapInfo.Update(arcMap, null);
        }

        #endregion 基本方法

        #region 菜单栏操作

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingDialog(arcMap) { Owner = this }.ShowDialog();
        }

        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            CommonDialog.ShowOkDialogAsync("关于", "开发人员：autodotua", "github:https://github.com/autodotua/MapBoard");
        }

        private void ClearHistoriesButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (MapLayerInfo layer in arcMap.Layers)
            {
                layer.Histories.Clear();
            }
        }

        private void MeasureAreaMenuItem_Click(object sender, RoutedEventArgs e)
        {
            arcMap.Editor.MeasureArea();
        }

        private void MeasureLengthMenuItem_Click(object sender, RoutedEventArgs e)
        {
            arcMap.Editor.MeasureLength();
        }

        private void GpxMenu_Click(object sender, RoutedEventArgs e)
        {
            new GpxToolbox.UI.MainWindow().Show();
        }

        private void TileMenu_Click(object sender, RoutedEventArgs e)
        {
            new TileDownloaderSplicer.UI.MainWindow().Show();
        }

        #endregion 菜单栏操作

        #region 样式设置区事件

        private async void CreateLayerButtonClick(object sender, RoutedEventArgs e)
        {
            await new CreateLayerDialog(arcMap.Layers).ShowAsync();
        }

        private async void ApplyStyleButtonClick(object sender, RoutedEventArgs e)
        {
            await layerSettings.SetStyleFromUI();
            arcMap.Layers.Save();
        }

        private void BrowseModeButtonClick(object sender, RoutedEventArgs e)
        {
            dataGrid.SelectedItem = null;
        }

        #endregion 样式设置区事件

        #region 导入导出区事件

        private void ExportMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Parameters.DataPath) || !Directory.EnumerateFiles(Parameters.DataPath).Any())
            {
                SnakeBar.ShowError("没有任何数据");
                return;
            }
            canClosing = false;
            DoAsync(async () =>
            {
                await IOUtility.ExportMapAsync(arcMap, arcMap.Layers, (ExportMapType)int.Parse((sender as FrameworkElement).Tag as string));
            });
            canClosing = true;
        }

        private void ExportMenu_Click(object sender, SplitButtonClickEventArgs e)
        {
            ExportMenu_Click(sender, (RoutedEventArgs)null);
        }

        private void ImportMenu_Click(object sender, RoutedEventArgs e)
        {
            DoAsync(async () =>
            {
                await IOUtility.ImportMapAsync(arcMap.Layers, (ImportMapType)int.Parse((sender as FrameworkElement).Tag as string));
            });
        }

        private void ImportMenu_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            ImportMenu_Click(sender, (RoutedEventArgs)null);
        }

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

        private async void DrawButtonsClick(SplitButton sender, SplitButtonClickEventArgs args)
        {
            await StartDraw(sender);
        }

        private async void DrawButtonsClick(object sender, RoutedEventArgs e)
        {
            await StartDraw(sender);
        }

        private async void SelectToggleButtonClick(object sender, RoutedEventArgs e)
        {
            await arcMap.Selection.SelectRectangleAsync();
        }

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

        private void ListItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            layerListHelper.ShowContextMenu();
        }

        private void lvw_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            layerListHelper.RightButtonClickToSelect(e);
        }

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