using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common;

using MapBoard.Common;

using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Component;
using MapBoard.Main.UI.Dialog;
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
        private LayerCollection layers;

        public LayerCollection Layers
        {
            get => layers;
            private set => this.SetValueAndNotify(ref layers, value, nameof(Layers));
        }

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
            await IOUtility.DropFilesAsync(files);
        }

        private void RegistEvents()
        {
            arcMap.Selection.SelectedFeatures.CollectionChanged += (p1, p2) => JudgeControlsEnable();
            BoardTaskManager.BoardTaskChanged += (s, e) => JudgeControlsEnable();
            LayerCollection.Instance.LayerVisibilityChanged += (s, e) => JudgeControlsEnable();

            var lvwHelper = new LayerDataGridHelper(dataGrid);
            lvwHelper.EnableDragAndDropItem();

            LayerCollection.Instance.PropertyChanged += (p1, p2) =>
              {
                  if (p2.PropertyName == nameof(LayerCollection.Instance.Selected) && !changingStyle)
                  {
                      dataGrid.SelectedItem = LayerCollection.Instance.Selected;
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
            LayerCollection.Instance.Save();
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit
                || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw)
            {
                await CommonDialog.ShowErrorDialogAsync("请先结束绘制");
                return;
            }
            Hide();
            await Package.BackupAsync();
            closing = true;
            Close();
        }

        #endregion 窗体启动与关闭

        private bool changingStyle = false;

        private async void ApplyStyleButtonClick(object sender, RoutedEventArgs e)
        {
            await Layersetting.SetStyleFromUI();
            LayerCollection.Instance.Save();
        }

        private void BrowseModeButtonClick(object sender, RoutedEventArgs e)
        {
            dataGrid.SelectedItem = null;
        }

        private async void CreateStyleButtonClick(object sender, RoutedEventArgs e)
        {
            //await CommonDialog.ShowSelectItemDialogAsync("请选择需要创建的图层类型", new DialogItem[]
            //  {
            //    new DialogItem("点","零维",async()=>await LayerUtility.CreateLayerAsync(GeometryType.Point)),
            //    new DialogItem("折线","一维（线）",async()=>await LayerUtility.CreateLayerAsync(GeometryType.Polyline)),
            //    new DialogItem("多边形","二维（面）",async()=>await LayerUtility.CreateLayerAsync(GeometryType.Polygon)),
            //  });
            await new CreateLayerDialog().ShowAsync();
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
            await IOUtility.ExportMapAsync();
        }

        private async void ImportBtnClick(object sender, RoutedEventArgs e)
        {
            await DoAsync(IOUtility.ImportLayerAsync);
        }

        private void JudgeControlsEnable()
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw
                || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit
                || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
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
            btnApplyStyle.IsEnabled = btnBrowseMode.IsEnabled = Layersetting.IsEnabled = LayerCollection.Instance.Selected != null;

            if (IsLoaded)
            {
                btnSelect.IsEnabled = LayerCollection.Instance.Selected != null;
                grdButtons.IsEnabled = LayerCollection.Instance.Selected == null || LayerCollection.Instance.Selected.LayerVisible;

                var buttons = grdButtons.Children.OfType<SplitButton>();
                buttons.ForEach(p => p.Visibility = Visibility.Collapsed);
                if (LayerCollection.Instance.Selected != null)
                {
                    switch (LayerCollection.Instance.Selected.Type)
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
            changingStyle = true;
            if (dataGrid.SelectedItems.Count == 1)
            {
                Layers.Selected = dataGrid.SelectedItem as LayerInfo;
            }
            else
            {
                Layers.Selected = null;
            }
            changingStyle = false;
            JudgeControlsEnable();
            Layersetting.ResetLayerSettingUI();
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
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
            {
                arcMap.Selection.ClearSelection();
            }

            var mode = ButtonsMode[text];
            arcMap.Editor.CurrentDrawMode = mode;
            await arcMap.Editor.DrawAsync(mode);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            LayerCollection.LayerInstanceChanged += (p1, p2) =>
            {
                Layers = LayerCollection.Instance;
            };
            await arcMap.LoadBasemapAsync();
            await LayerCollection.LoadInstanceAsync();
            await arcMap.ZoomToLastExtent();
            RegistEvents();
            layerListHelper = new LayerListPanelHelper(dataGrid, this);

            JudgeControlsEnable();

            if (LayerCollection.Instance.Selected != null && LayerCollection.Instance.Selected.FeatureCount > 0)
            {
                Layersetting.ResetLayerSettingUI();
            }
            FeatureUtility.FeaturesGeometryChanged += FeatureUtility_FeaturesGeometryChanged;
        }

        private void FeatureUtility_FeaturesGeometryChanged(object sender, FeaturesGeometryChangedEventArgs e)
        {
            if (e.AddedFeatures != null || e.DeletedFeatures != null || e.ChangedFeatures != null)
            {
                e.Layer.UpdateFeatureCount();
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
    }
}