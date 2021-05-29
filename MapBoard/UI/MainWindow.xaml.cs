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
using MapBoard.Main.UI.Panel;
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

namespace MapBoard.Main.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        #region 字段和属性

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

        public Config Config => Config.Instance;
        //private MapLayerCollection layers;

        //public MapLayerCollection Layers
        //{
        //    get => layers;
        //    private set => this.SetValueAndNotify(ref layers, value, nameof(Layers));
        //}

        private LayerListPanelHelper layerListHelper;

        #endregion 字段和属性

        #region 窗体启动与关闭

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RegistEvents()
        {
            arcMap.Selection.SelectedFeatures.CollectionChanged += (p1, p2) => JudgeControlsEnable();
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

        private bool closing = false;
        private bool canClosing = true;

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

        #endregion 窗体启动与关闭

        private bool changingSelection = false;

        private async void ApplyStyleButtonClick(object sender, RoutedEventArgs e)
        {
            await layerSettings.SetStyleFromUI();
            arcMap.Layers.Save();
        }

        private void BrowseModeButtonClick(object sender, RoutedEventArgs e)
        {
            dataGrid.SelectedItem = null;
        }

        private async void CreateLayerButtonClick(object sender, RoutedEventArgs e)
        {
            await new CreateLayerDialog(arcMap.Layers).ShowAsync();
        }

        private async void DrawButtonsClick(SplitButton sender, SplitButtonClickEventArgs args)
        {
            await StartDraw(sender);
        }

        private async void DrawButtonsClick(object sender, RoutedEventArgs e)
        {
            await StartDraw(sender);
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

        private void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Config.DataPath))
            {
                IOUtility.OpenFileOrFolder(Config.DataPath);
            }
            else
            {
                IOUtility.OpenFileOrFolder(FzLib.Program.App.ProgramDirectoryPath);
            }
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

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            await InitializeAsync();
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

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingDialog() { Owner = this }.ShowDialog();
        }

        private void GpxMenu_Click(object sender, RoutedEventArgs e)
        {
            new GpxToolbox.UI.MainWindow().Show();
        }

        private void TileMenu_Click(object sender, RoutedEventArgs e)
        {
            new TileDownloaderSplicer.UI.MainWindow().Show();
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

        private void MeasureLengthMenuItem_Click(object sender, RoutedEventArgs e)
        {
            arcMap.Editor.MeasureLength();
        }

        private void MeasureAreaMenuItem_Click(object sender, RoutedEventArgs e)
        {
            arcMap.Editor.MeasureArea();
        }

        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            CommonDialog.ShowOkDialogAsync("关于", "开发人员：autodotua", "github:https://github.com/autodotua/MapBoard");
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

        private void ImportMenu_Click(object sender, RoutedEventArgs e)
        {
            DoAsync(async () =>
            {
                await IOUtility.ImportMapAsync(arcMap.Layers, (ImportMapType)int.Parse((sender as FrameworkElement).Tag as string));
            });
        }

        private void ExportMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Config.DataPath))
            {
                SnakeBar.ShowError("数据目录" + Config.DataPath + "不存在");
                return;
            }
            canClosing = false;
            DoAsync(async () =>
            {
                await IOUtility.ExportMapAsync(arcMap, arcMap.Layers, (ExportMapType)int.Parse((sender as FrameworkElement).Tag as string));
            });
            canClosing = true;
        }

        private void ClearHistoriesButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (MapLayerInfo layer in arcMap.Layers)
            {
                layer.Histories.Clear();
            }
        }
    }
}