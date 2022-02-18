using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib;
using FzLib.WPF.Dialog;
using MapBoard.IO;
using MapBoard.UI.Dialog;
using MapBoard.Mapping;
using MapBoard.Util;
using ModernWpf.Controls;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Geometry = Esri.ArcGISRuntime.Geometry.Geometry;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using Microsoft.WindowsAPICodePack.FzExtension;
using Microsoft.WindowsAPICodePack.Dialogs;
using FzLib.WPF;

namespace MapBoard.UI.Bar
{
    /// <summary>
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class SelectionBar : BarBase
    {
        private FeatureAttributeCollection attributes;

        private SelectFeatureDialog selectFeatureDialog;

        public SelectionBar()
        {
            InitializeComponent();
        }

        public override FeatureAttributeCollection Attributes => attributes;

        public bool IsLayerEditable =>
            (Layers?.Selected) != null
            && Layers?.Selected?.CanEdit==true;

        public string Message { get; set; } = "正在编辑";

        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
            this.GetWindow().SizeChanged += (p1, p2) => selectFeatureDialog?.ResetLocation();
            this.GetWindow().LocationChanged += (p1, p2) => selectFeatureDialog?.ResetLocation();
            MapView.Selection.CollectionChanged += SelectedFeaturesChanged;
        }

        private void BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTask.Select)
            {
                SelectedFeaturesChanged(null, null);
                Expand();
            }
            else
            {
                Collapse();
            }
        }

        private void BtnExportClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected;
            var features = MapView.Selection.SelectedFeatures.ToArray();
            List<(string header, string desc, Func<Task> action, bool visiable)> menus = new List<(string header, string desc, Func<Task> action, bool visiable)>()
           {
                ("导出到CSV表格","将图形导出为CSV表格",ToCsvAsync, true),
                ("导出到GeoJSON","将图形导出为GeoJSON",ToGeoJsonAsync, true),
            };
            OpenMenus(menus, sender as UIElement, header => $"正在{header}");

            Task ToCsvAsync()
            {
                return ExportBase(new FileFilterCollection().Add("Csv表格", "csv"), async path => await Csv.ExportAsync(path, MapView.Selection.SelectedFeatures));
            }
            Task ToGeoJsonAsync()
            {
                return ExportBase(new FileFilterCollection().Add("GeoJSON", "geojson"), async path => await GeoJson.ExportAsync(path, MapView.Selection.SelectedFeatures));
            }
        }

        private void BtnMenuClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected as IEditableLayerInfo;
            if (layer == null)
            {
                return;
            }
            var features = MapView.Selection.SelectedFeatures.ToArray();
            List<(string header, string desc, Func<Task> action, bool visiable)> menus = new List<(string header, string desc, Func<Task> action, bool visiable)>()
           {
                ("合并","将多个图形合并为一个具有多个部分的图形",
                UnionAsync,
                layer.GeometryType is GeometryType.Polygon or GeometryType.Polyline or GeometryType.Multipoint
                    &&IsLayerEditable
                    && features.Length>1
                ),
                ("分离","将拥有多个部分的图形分离为单独的图形",
                SeparateAsync
                ,layer.GeometryType is GeometryType.Polygon or GeometryType.Polyline
                    &&IsLayerEditable
                ),
                ("连接","将折线的端点互相连接",
                LinkAsync,
                layer.GeometryType==GeometryType.Polyline
                    && features.Length>1
                    && features.All(p=>(p.Geometry as Polyline).Parts.Count==1)
                    &&IsLayerEditable
                ),
                ("反转","交换点的顺序",ReverseAsync,
                layer.GeometryType is GeometryType.Polyline or GeometryType.Polygon&&IsLayerEditable),
                ("加密","在每两个折点之间添加更多的点",DensifyAsync,
                layer.GeometryType is GeometryType.Polyline or GeometryType.Polygon&&IsLayerEditable),
                ("简化","删除部分折点，降低图形的复杂度",SimplifyAsync,
                layer.GeometryType is GeometryType.Polyline or GeometryType.Polygon&&IsLayerEditable),
                ("平滑","在适当的位置添加节点，使图形更平滑",SmoothAsync,
                layer.GeometryType is GeometryType.Polyline or GeometryType.Polygon or GeometryType.Multipoint&&IsLayerEditable),
                ("建立副本","在原位置创建拥有相同图形和属性的要素",CreateCopyAsync, IsLayerEditable),
                ("字段赋值","批量为选中的图形赋予新的属性",CopyAttributeAsync, IsLayerEditable),
                ("缓冲区","为选中的图形建立缓冲区",BufferAsync, true),
            };
            OpenMenus(menus, sender as UIElement, header => $"正在进行{header}操作");
            async Task BufferAsync()
            {
                var dialog = new BufferDialog(MapView.Layers);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await LayerUtility.BufferAsync(layer, MapView.Layers, dialog.ToNewLayer ? null : dialog.TargetLayer, dialog.Distance, dialog.Union, features);
                }
            }
            async Task SeparateAsync()
            {
                var result = await FeatureUtility.SeparateAsync(layer, features);
                if (result == null || result.Count == 0)
                {
                    await CommonDialog.ShowErrorDialogAsync("选中的图形不需要分离");
                }
                else
                {
                    SnakeBar.Show($"已分离出{result.Count}个图形");
                    MapView.Selection.ClearSelection();
                }
            }
            async Task UnionAsync()
            {
                var result = await FeatureUtility.UnionAsync(layer, features);
                MapView.Selection.Select(result, true);
                SnakeBar.Show($"合并完成");
            }

            async Task LinkAsync()
            {
                List<SelectDialogItem> typeList = new List<SelectDialogItem>();
                bool headToHead = false;
                bool reverse = false;
                bool auto = false;
                typeList.Add(new SelectDialogItem("自动", "根据选择的顺序自动判断需要连接的点", () => auto = true));
                if (MapView.Selection.SelectedFeatures.Count == 2)
                {
                    typeList.Add(new SelectDialogItem("起点相连", "起始点与起始点相连接", () => headToHead = true));
                    typeList.Add(new SelectDialogItem("终点相连", "终结点与终结点相连接", () => headToHead = reverse = true));
                    typeList.Add(new SelectDialogItem("头尾相连", "第一个要素的终结点与第二个要素的起始点相连接", null));
                    typeList.Add(new SelectDialogItem("头尾相连（反转）", "第一个要素的起始点与第二个要素的终结点相连接", () => reverse = true));
                }
                else
                {
                    typeList.Add(new SelectDialogItem("头尾相连", "每一个要素的终结点与前一个要素的起始点相连接", null));
                    typeList.Add(new SelectDialogItem("头尾相连（反转）", "每一个要素的起始点与前一个要素的终结点相连接", () => reverse = true));
                }

                int result = await CommonDialog.ShowSelectItemDialogAsync(
                    "请选择连接类型", typeList, "查看图形起点和终点", async () =>
                     {
                         await MapView.Overlay.ShowHeadAndTailOfFeatures(features);
                         SnakeBar snake = new SnakeBar(this.GetWindow())
                         {
                             ShowButton = true,
                             ButtonContent = "继续",
                             Duration = Duration.Forever
                         };
                         snake.ButtonClick += async (p1, p2) =>
                          {
                              MapView.Overlay.ClearHeadAndTail();
                              snake.Hide();
                              await LinkAsync();
                          };

                         snake.ShowMessage("正在显示图形起点和终点");
                     });

                if (result < 0)
                {
                    return;
                }
                Feature feature;
                if (auto)
                {
                    feature = await FeatureUtility.AutoLinkAsync(layer, features);
                }
                else
                {
                    feature = await FeatureUtility.LinkAsync(layer, features, headToHead, reverse);
                }
                MapView.Selection.Select(feature, true);
            }

            async Task ReverseAsync()
            {
                await FeatureUtility.ReverseAsync(layer, features);
                SnakeBar.Show($"反转完成");
            }

            async Task DensifyAsync()
            {
                double? num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大间隔（米）");
                if (num.HasValue)
                {
                    await FeatureUtility.DensifyAsync(layer, features, num.Value);
                    SnakeBar.Show($"加密完成");
                }
            }
            async Task SmoothAsync()
            {
                var dialog = new SmoothDialog();
                if (await dialog.ShowAsync(ContentDialogPlacement.InPlace) == ContentDialogResult.Primary)
                {
                    await FeatureUtility.Smooth(layer, features, dialog.PointsPerSegment, dialog.Level);
                    if (dialog.Simplify)
                    {
                        await FeatureUtility.GeneralizeSimplifyAsync(layer, features, dialog.MaxDeviation);
                    }
                }
            }
            async Task SimplifyAsync()
            {
                int i = await CommonDialog.ShowSelectItemDialogAsync("请选择简化方法", new SelectDialogItem[]
                   {
                        new SelectDialogItem("间隔取点法","或每几个点保留一点"),
                        new SelectDialogItem("垂距法","中间隔一个点连接两个点，然后计算垂距或角度，在某一个值以内则可以删除中间间隔的点"),
                        new SelectDialogItem("最大偏离法","保证新的图形与旧图形之间的距离不超过一个值")
                   });
                if (i >= 0)
                {
                    double? num = null;
                    num = i switch
                    {
                        0 => await CommonDialog.ShowDoubleInputDialogAsync("请输入间隔几点取一点"),
                        1 => await CommonDialog.ShowDoubleInputDialogAsync("请输入最大垂距（米）"),
                        2 => await CommonDialog.ShowDoubleInputDialogAsync("请输入最大垂距（米）"),
                        _ => throw new NotImplementedException()
                    };
                    if (num.HasValue)
                    {
                        var task = i switch
                        {
                            0 => FeatureUtility.IntervalTakePointsSimplifyAsync(layer, features, num.Value),
                            1 => FeatureUtility.VerticalDistanceSimplifyAsync(layer, features, num.Value),
                            2 => FeatureUtility.GeneralizeSimplifyAsync(layer, features, num.Value),
                            _ => throw new NotImplementedException()
                        };
                        await task;
                    }
                }
            }
            async Task CreateCopyAsync()
            {
                MapView.Selection.ClearSelection();
                var newFeatures = await FeatureUtility.CreateCopyAsync(layer, features);
                MapView.Selection.Select(newFeatures);
            }
            async Task CopyAttributeAsync()
            {
                var dialog = new CopyAttributesDialog(layer);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    ItemsOperationErrorCollection errors = null;

                    switch (dialog.Type)
                    {
                        case CopyAttributesType.Field:
                            errors = await AttributeUtility.CopyAttributesAsync(layer, features, dialog.SourceField, dialog.TargetField, dialog.DateFormat);
                            break;

                        case CopyAttributesType.Const:
                            errors = await AttributeUtility.SetAttributesAsync(layer, features, dialog.TargetField, dialog.Text, false, dialog.DateFormat);

                            break;

                        case CopyAttributesType.Custom:
                            errors = await AttributeUtility.SetAttributesAsync(layer, features, dialog.TargetField, dialog.Text, true, dialog.DateFormat);
                            break;

                        default:
                            break;
                    }
                    await ItemsOperaionErrorsDialog.TryShowErrorsAsync("部分属性复制失败", errors);
                }
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Selection.ClearSelection();
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            await (this.GetWindow() as MainWindow).DoAsync(async () =>
            {
                SelectLayerDialog dialog = new SelectLayerDialog(MapView.Layers,
                    p => p.CanEdit && p.GeometryType == MapView.Layers.Selected.GeometryType,
                    true);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    bool copy = Layers.Selected is IEditableLayerInfo ?
                    await CommonDialog.ShowYesNoDialogAsync("是否保留原图层中选中的图形？") : true;

                    await FeatureUtility.CopyOrMoveAsync(Layers.Selected, dialog.SelectedLayer as IEditableLayerInfo, MapView.Selection.SelectedFeatures.ToArray(), copy);
                    MapView.Selection.ClearSelection();
                    Layers.Selected = dialog.SelectedLayer;
                }
            }, "正在复制图形");
        }

        private async void CutButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(Layers.Selected is IEditableLayerInfo);
            var features = MapView.Selection.SelectedFeatures.ToArray();
            var line = await MapView.Editor.GetPolylineAsync();
            if (line != null)
            {
                await (this.GetWindow() as MainWindow).DoAsync(async () =>
                {
                    var result = await FeatureUtility.CutAsync(Layers.Selected as IEditableLayerInfo, features, line);
                    SnakeBar.Show($"已分割为{result.Count}个图形");
                }, "正在分割", true);
            }
        }

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(Layers.Selected is IEditableLayerInfo);
            await (this.GetWindow() as MainWindow).DoAsync(async () =>
            {
                await FeatureUtility.DeleteAsync(Layers.Selected as IEditableLayerInfo, MapView.Selection.SelectedFeatures.ToArray());
                MapView.Selection.ClearSelection();
            }, "正在删除", true);
        }

        private async void EditButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(MapView.Selection.SelectedFeatures.Count == 1);
            Debug.Assert(Layers.Selected is IEditableLayerInfo);
            var feature = MapView.Selection.SelectedFeatures.First();
            MapView.Selection.ClearSelection();
            await MapView.Editor.EditAsync(Layers.Selected as IEditableLayerInfo, feature);
        }

        private async Task ExportBase(FileFilterCollection filter, Func<string, Task> task)
        {
            string path = filter.CreateSaveFileDialog()
                        .SetDefault(MapView.Selection.SelectedFeatures.Count + "个图形")
                        .SetParent(this.GetWindow())
                        .GetFilePath();
            if (path != null)
            {
                await task(path);
                IOUtility.ShowExportedSnackbarAndClickToOpenFolder(path, this.GetWindow());
            }
        }

        private void Layers_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MapLayerCollection.Selected))
            {
                this.Notify(nameof(IsLayerEditable));
            }
        }

        private void OpenMenus(List<(string header, string desc, Func<Task> action, bool visiable)> menus, UIElement parent, Func<string, string> getMessage)
        {
            ContextMenu menu = new ContextMenu();

            foreach (var (header, desc, action, visiable) in menus)
            {
                if (visiable)
                {
                    StackPanel content = new StackPanel();
                    content.Children.Add(new TextBlock()
                    {
                        Text = header,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 4, 0, 4)
                    });
                    content.Children.Add(new TextBlock()
                    {
                        Text = desc,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                    MenuItem item = new MenuItem() { Header = content };
                    item.Click += async (p1, p2) =>
                    {
                        try
                        {
                            await (this.GetWindow() as MainWindow).DoAsync(action, getMessage(header));
                        }
                        catch (Exception ex)
                        {
                            App.Log.Error("执行菜单失败", ex);
                            await CommonDialog.ShowErrorDialogAsync(ex);
                        }
                    };
                    menu.Items.Add(item);
                }
            }

            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = parent;
            menu.IsOpen = true;
        }

        private async void SelectedFeaturesChanged(object sender, EventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Select)
            {
                return;
            }
            int count = MapView.Selection.SelectedFeatures.Count;
            btnEdit.IsEnabled = count == 1 && IsLayerEditable;
            //btnMoreAttributes.IsEnabled = count == 1;
            var layer = Layers.Selected;
            btnCut.IsEnabled = (layer.GeometryType == GeometryType.Polygon
                || layer.GeometryType == GeometryType.Polyline)
                && IsLayerEditable;
            btnDelete.IsEnabled = IsLayerEditable;
            btnMenu.IsEnabled = IsLayerEditable;
            attributes = count != 1 ?
                null : FeatureAttributeCollection.FromFeature(layer, MapView.Selection.SelectedFeatures.First());

            if (count > 1 && (selectFeatureDialog == null || selectFeatureDialog.IsClosed))
            {
                selectFeatureDialog = new SelectFeatureDialog(this.GetWindow(), MapView, MapView.Layers);
                selectFeatureDialog.Show(); ;
            }

            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count}个图形");
            Message = sb.ToString();
            await Task.Run(() =>
            {
                try
                {
                    switch (layer.GeometryType)
                    {
                        case GeometryType.Point when count == 1:
                            var point = MapView.Selection.SelectedFeatures.First().Geometry as MapPoint;
                            sb.Append($"，经度={point.X:0.0000000}，纬度={point.Y:0.0000000}");
                            break;

                        case GeometryType.Envelope:
                            return;

                        case GeometryType.Polyline:
                            double length = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetLength());
                            sb.Append("，长度：" + NumberConverter.MeterToFitString(length));

                            break;

                        case GeometryType.Polygon:
                            double length2 = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetLength());
                            double area = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetArea());
                            sb.Append("，周长：" + NumberConverter.MeterToFitString(length2));
                            sb.Append("，面积：" + NumberConverter.SquareMeterToFitString(area));

                            break;

                        case GeometryType.Multipoint:
                            int pointCount = MapView.Selection.SelectedFeatures
                            .Select(p => (p.Geometry as Multipoint).Points.Count).Sum();
                            sb.Append($"，{pointCount}个点");
                            break;

                        default:
                            return;
                    }
                }
                catch (InvalidOperationException)
                {
                    //当MapView.Selection.SelectedFeatures集合发生改变时会抛出错误，此时不需要继续进行计算
                    return;
                }
            });
            Message = sb.ToString();

            this.Notify(nameof(Attributes));
        }

        private void ValueTextBlock_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tbk)
            {
                if (!string.IsNullOrEmpty(tbk.Text))
                {
                    Clipboard.SetText(tbk.Text);
                    SnakeBar.Show($"已复制“{tbk.Text}”到剪贴板");
                }
            }
        }
    }
}