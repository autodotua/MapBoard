using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.Program;
using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common;
using MapBoard.Common.Resource;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.Util;
using ModernWpf.Controls;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static FzLib.Basic.Loop;
using static MapBoard.Main.UI.Dialog.MultiLayersOperationDialog;

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

        /// <summary>
        /// 控制在执行耗时工作时控件的可用性
        /// </summary>
        private bool controlsEnable = true;

        public Config Config => Config.Instance;
        public LayerCollection Layers => LayerCollection.Instance;

        #endregion 字段和属性

        #region 窗体启动与关闭

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            RegistEvents();
            LayerCollection.LayerInstanceChanged += (p1, p2) =>
              {
                  Notify(nameof(Layers));
              };
        }

        protected async override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files) || files.Length == 0)
            {
                return;
            }
            await IOUtility.DropFiles(files);
        }

        private void RegistEvents()
        {
            arcMap.Selection.SelectedFeatures.CollectionChanged += (p1, p2) => JudgeControlsEnable();
            BoardTaskManager.BoardTaskChanged += (s, e) => JudgeControlsEnable();
            LayerCollection.Instance.LayerVisibilityChanged += (s, e) => JudgeControlsEnable();

            var lvwHelper = new DataGridHelper<LayerInfo>(dataGrid);
            lvwHelper.EnableDragAndDropItem();

            LayerCollection.Instance.PropertyChanged += (p1, p2) =>
              {
                  if (p2.PropertyName == nameof(LayerCollection.Instance.Selected) && !changingStyle)
                  {
                      dataGrid.SelectedItem = LayerCollection.Instance.Selected;
                  }
              };
        }

        private async void WindowClosing(object sender, CancelEventArgs e)
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit)
            {
                await arcMap.Edit.StopEditing();
            }

            Config.Save();
            LayerCollection.Instance.Save();
        }

        #endregion 窗体启动与关闭

        private bool changingStyle = false;

        private async void ApplyStyleButtonClick(object sender, RoutedEventArgs e)
        {
            await Layersetting.SetStyleFromUI();
            LayerCollection.Instance.Save();
        }

        private void BatchOperationButtonClick(object sender, RoutedEventArgs e)
        {
            new MultiLayersOperationDialog().Show();
        }

        private void BrowseModeButtonClick(object sender, RoutedEventArgs e)
        {
            dataGrid.SelectedItem = null;
        }

        private async void CreateStyleButtonClick(object sender, RoutedEventArgs e)
        {
            await CommonDialog.ShowSelectItemDialogAsync("请选择需要创建的图层类型", new DialogItem[]
              {
                new DialogItem(){Title="点",SelectAction=()=>LayerUtility.CreateLayer(GeometryType.Polyline)},
                new DialogItem(){Title="多点",SelectAction=()=>LayerUtility.CreateLayer(GeometryType.Multipoint)},
                new DialogItem(){Title="线",SelectAction=()=>LayerUtility.CreateLayer(GeometryType.Polyline)},
                new DialogItem(){Title="面",SelectAction=()=>LayerUtility.CreateLayer(GeometryType.Polygon)},
              });
        }

        private void DeleteLayer()
        {
            if (dataGrid.SelectedItems.Count == 0)
            {
                SnakeBar.ShowError("没有选择任何样式");
                return;
            }
            foreach (LayerInfo layer in dataGrid.SelectedItems.Cast<LayerInfo>().ToArray())
            {
                LayerUtility.RemoveLayer(layer, true);
            }
        }

        private async void DrawButtonsClick(ModernWpf.Controls.SplitButton sender, ModernWpf.Controls.SplitButtonClickEventArgs args)
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
            await IOUtility.ExportMap();
        }

        private async void ImportBtnClick(object sender, RoutedEventArgs e)
        {
            await Do(IOUtility.ImportLayer);
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
                            arcMap.Drawing.CurrentDrawMode = SketchCreationMode.Multipoint;
                            break;

                        case GeometryType.Point:
                            splBtnPoint.Visibility = Visibility.Visible;
                            arcMap.Drawing.CurrentDrawMode = SketchCreationMode.Point;
                            break;

                        case GeometryType.Polyline:
                            splBtnPolyline.Visibility = Visibility.Visible;
                            arcMap.Drawing.CurrentDrawMode = SketchCreationMode.Polyline;
                            break;

                        case GeometryType.Polygon:
                            splBtnPolygon.Visibility = Visibility.Visible;
                            arcMap.Drawing.CurrentDrawMode = SketchCreationMode.Polygon;
                            break;
                    }
                }
                else
                {
                    arcMap.Drawing.CurrentDrawMode = null;
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
            ContextMenu menu = new ContextMenu();
            List<(string header, Action action, bool visiable)> menus = null;
            LayerInfo layer = LayerCollection.Instance.Selected;
            LayerInfo[] Layers = dataGrid.SelectedItems.Cast<LayerInfo>().ToArray();
            if (dataGrid.SelectedItems.Count == 1)
            {
                menus = new List<(string header, Action action, bool visiable)>()
               {
                    ("复制",LayerUtility. CopyFeatures,true),
                    ("建立缓冲区",LayerUtility.Buffer,layer.Type==GeometryType.Polyline || layer.Type==GeometryType.Point|| layer.Type==GeometryType.Multipoint),
                    ("删除",DeleteLayer,true),
                    ("新建副本",LayerUtility. CreateCopy,true),
                    ("缩放到图层", ZoomToLayer,LayerCollection.Instance.Selected.FeatureCount > 0),
                    ("坐标转换",CoordinateTransformate,true),
                    ("设置时间范围",SetTimeExtent,layer.Table.Fields.Any(p=>p.FieldType==FieldType.Date && p.Name==Resource.TimeExtentFieldName)),
                    ("导入",async()=>await IOUtility.ImportFeature(),true),
                    ("导出",  ExportSingle,true),
               };
            }
            else
            {
                menus = new List<(string header, Action action, bool visiable)>()
               {
                    ("合并",async()=>await LayerUtility. Union(Layers),Layers.Select(p=>p.Type).Distinct().Count()==1),
                    ("删除",DeleteLayer,true),
                };
            }

            foreach (var (header, action, visiable) in menus)
            {
                if (visiable)
                {
                    MenuItem item = new MenuItem() { Header = header };
                    item.Click += (p1, p2) => action();
                    menu.Items.Add(item);
                }
            }
            if (menu.Items.Count > 0)
            {
                menu.IsOpen = true;
            }

            async void ExportSingle()
            {
                await Do(IOUtility.ExportLayer);
            }

            async void ZoomToLayer()
            {
                try
                {
                    await arcMap.ZoomToGeometry(await layer.Table.QueryExtentAsync(new QueryParameters()));
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "操作失败，可能是不构成有面积的图形");
                }
            }

            async void CoordinateTransformate()
            {
                CoordinateTransformationDialog dialog = new CoordinateTransformationDialog();
                if (dialog.ShowDialog() == true)
                {
                    await Do(async () =>
                     {
                         string from = dialog.SelectedCoordinateSystem1;
                         string to = dialog.SelectedCoordinateSystem2;
                         await LayerUtility.CoordinateTransformate(LayerCollection.Instance.Selected, from, to);
                     });
                }
            }

            async void SetTimeExtent()
            {
                DateRangeDialog dialog = new DateRangeDialog(layer);
                if (dialog.ShowDialog() == true)
                {
                    await LayerUtility.SetTimeExtent(layer);
                }
            }
        }

        private void ListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteLayer();
            }
        }

        private void lvw_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dataGrid.SelectedItems.Count > 1)
            {
                return;
            }
            var obj = e.OriginalSource as FrameworkElement;
            while (obj != null && !(obj.DataContext is LayerInfo))
            {
                obj = obj.Parent as FrameworkElement;
            }
            var layer = obj.DataContext as LayerInfo;
            if (layer != null)
            {
                dataGrid.SelectedItem = layer;
            }
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
            await arcMap.Selection.StartSelect(SketchCreationMode.Rectangle);
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
                await arcMap.Selection.StopFrameSelect(false);
            }

            var mode = ButtonsMode[text];
            arcMap.Drawing.CurrentDrawMode = mode;
            await arcMap.Drawing.StartDraw(mode);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            JudgeControlsEnable();

            if (LayerCollection.Instance.Selected != null && LayerCollection.Instance.Selected.FeatureCount > 0)
            {
                Layersetting.ResetLayerSettingUI();
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingDialog() { Owner = this }.ShowDialog();
        }

        private void GpxMenu_Click(object sender, RoutedEventArgs e)
        {
            new GpxToolbox.MainWindow().Show();
        }

        private void TileMenu_Click(object sender, RoutedEventArgs e)
        {
            new TileDownloaderSplicer.MainWindow().Show();
        }

        public async Task Do(Func<Task> action)
        {
            loading.IsActive = true;
            try
            {
                await action();
            }
            catch
            {
                throw;
            }
            finally
            {
                loading.IsActive = false;
            }
        }
    }
}