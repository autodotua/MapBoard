﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.UI.Dialog;
using FzLib.UI.Extension;
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
    public partial class MainWindow : MainWindowBase
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

        protected async override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files) || files.Length == 0)
            {
                return;
            }
            await IOUtility.DropFilesAsync(files, arcMap.Layers);
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

        private async void WindowClosing(object sender, CancelEventArgs e)
        {
            if (closing)
            {
                return;
            }
            e.Cancel = true;
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

        private async void ExportBtnClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Config.DataPath))
            {
                SnakeBar.ShowError("数据目录" + Config.DataPath + "不存在");
                return;
            }
            await IOUtility.ExportMapAsync(arcMap, arcMap.Layers);
        }

        private async void ImportBtnClick(object sender, RoutedEventArgs e)
        {
            await DoAsync(() => IOUtility.ImportPackageAsync(arcMap.Layers));
        }

        private async void AddBtnClick(object sender, RoutedEventArgs e)
        {
            await DoAsync(() => IOUtility.AddLayerAsync(arcMap.Layers));
        }

        private void JudgeControlsEnable()
        {
            if (arcMap.CurrentTask == BoardTask.Draw
                || arcMap.CurrentTask == BoardTask.Select)
            {
                grdLeft.IsEnabled = false;
            }
            else
            {
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
                    switch (arcMap.Layers.Selected.Table.GeometryType)
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
                if (undoSnakeBar != null)
                {
                    undoSnakeBar.Hide();
                    undoSnakeBar = null;
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
                Process.Start(Config.DataPath);
            }
            else
            {
                Process.Start(FzLib.Program.App.ProgramDirectoryPath);
            }
        }

        private void SelectedLayerChanged(object sender, SelectionChangedEventArgs e)
        {
            changingSelection = true;
            if (dataGrid.SelectedItems.Count == 1)
            {
                arcMap.Layers.Selected = dataGrid.SelectedItem as LayerInfo;
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
            foreach (var bar in new BarBase[] { editBar, selectBar, measureBar })
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
                && arcMap.Layers.Selected.Table != null
                && arcMap.Layers.Selected.Table.NumberOfFeatures > 0)
            {
                layerSettings.ResetLayerSettingUI();
            }
            FeatureUtility.FeaturesGeometryChanged += FeatureUtility_FeaturesGeometryChanged;
        }

        private void FeatureUtility_FeaturesGeometryChanged(object sender, FeaturesGeometryChangedEventArgs e)
        {
            if (e.AddedFeatures != null || e.DeletedFeatures != null || e.ChangedFeatures != null)
            {
                e.Layer.NotifyFeatureChanged();
                if (undoSnakeBar != null)
                {
                    undoSnakeBar.Hide();
                }
                SnakeBar snake = new SnakeBar(this)
                {
                    Duration = TimeSpan.FromSeconds(10),
                    ButtonContent = "撤销",
                    ShowButton = true,
                };

                undoSnakeBar = snake;
                snake.ShowMessage("操作完成");
                snake.ButtonClick += async (p1, p2) =>
                 {
                     undoSnakeBar = null;
                     snake.Hide();
                     await DoAsync(async () =>
                      {
                          try
                          {
                              await UndoFeatureOperation(e);
                          }
                          catch (Exception ex)
                          {
                              await CommonDialog.ShowErrorDialogAsync(ex, "撤销失败");
                          }
                      });
                 };
            }
        }

        private SnakeBar undoSnakeBar = null;

        private async Task UndoFeatureOperation(FeaturesGeometryChangedEventArgs e)
        {
            if (e.AddedFeatures != null && e.AddedFeatures.Count > 0)
            {
                await e.Layer.Table.DeleteFeaturesAsync(e.AddedFeatures);
            }
            if (e.DeletedFeatures != null && e.DeletedFeatures.Count > 0)
            {
                await e.Layer.Table.AddFeaturesAsync(e.DeletedFeatures);
            }
            if (e.ChangedFeatures != null && e.ChangedFeatures.Count > 0)
            {
                foreach (var feature in e.ChangedFeatures.Keys)
                {
                    feature.Geometry = e.ChangedFeatures[feature];
                }
                await e.Layer.Table.UpdateFeaturesAsync(e.ChangedFeatures.Keys);
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
    }
}