using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using FzLib.Basic.Collection;
using FzLib.Extension;
using FzLib.WPF.Dialog;
using MapBoard.Common;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
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
using MapBoard.Main.UI.Map.Model;

namespace MapBoard.Main.UI.Bar
{
    /// <summary>
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class SelectionBar : BarBase
    {
        public SelectionBar()
        {
            InitializeComponent();
        }

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
            Application.Current.MainWindow.SizeChanged += (p1, p2) => selectFeatureDialog?.ResetLocation();
            Application.Current.MainWindow.LocationChanged += (p1, p2) => selectFeatureDialog?.ResetLocation();
            MapView.Selection.CollectionChanged += SelectedFeaturesChanged;
        }

        private SelectFeatureDialog selectFeatureDialog;

        private void SelectedFeaturesChanged(object sender, EventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Select)
            {
                return;
            }
            int count = MapView.Selection.SelectedFeatures.Count;
            btnRedraw.IsEnabled = count == 1;
            //btnMoreAttributes.IsEnabled = count == 1;
            MapLayerInfo layer = Layers.Selected;
            btnCut.IsEnabled = layer.GeometryType == GeometryType.Polygon
                || layer.GeometryType == GeometryType.Polyline;
            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count}个图形");
            if (layer.GeometryType == GeometryType.Polyline)//线
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetLength());
                sb.Append("，长度：" + Number.MeterToFitString(length));
            }
            else if (layer.GeometryType == GeometryType.Polygon)//面
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetLength());
                double area = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetArea());
                sb.Append("，周长：" + Number.MeterToFitString(length));
                sb.Append("，面积：" + Number.SquareMeterToFitString(area));
            }
            Message = sb.ToString();

            if (count > 1 && (selectFeatureDialog == null || selectFeatureDialog.IsClosed))
            {
                var mainWindow = Application.Current.MainWindow;
                selectFeatureDialog = new SelectFeatureDialog(MapView.Selection, MapView.Layers);
                selectFeatureDialog.Show();
            }

            if (count == 1)
            {
                attributes = FeatureAttributes.FromFeature(layer, MapView.Selection.SelectedFeatures.First());
            }
            else
            {
                attributes = null;
            }
            this.Notify(nameof(Attributes));
        }

        private FeatureAttributes attributes;

        public override FeatureAttributes Attributes => attributes;

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

        private string message = "正在编辑";

        public string Message
        {
            get => message;
            set
            {
                message = value;
                this.Notify(nameof(Message));
            }
        }

        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
            {
                await FeatureUtility.DeleteAsync(Layers.Selected, MapView.Selection.SelectedFeatures.ToArray());
                MapView.Selection.ClearSelection();
            }, true);
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
            {
                SelectLayerDialog dialog = new SelectLayerDialog(MapView.Layers);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    bool copy = await CommonDialog.ShowYesNoDialogAsync("是否保留原图层中选中的图形？");

                    await FeatureUtility.CopyOrMoveAsync(Layers.Selected, dialog.SelectedLayer, MapView.Selection.SelectedFeatures.ToArray(), copy);
                    MapView.Selection.ClearSelection();
                    Layers.Selected = dialog.SelectedLayer;
                }
            });
        }

        private async void CutButtonClick(object sender, RoutedEventArgs e)
        {
            var features = MapView.Selection.SelectedFeatures.ToArray();
            var line = await MapView.Editor.GetPolylineAsync();
            if (line != null)
            {
                await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
                {
                    await FeatureUtility.CutAsync(Layers.Selected, features, line);
                }, true);
            }
        }

        private async void EditButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(MapView.Selection.SelectedFeatures.Count == 1);
            var feature = MapView.Selection.SelectedFeatures.First();
            MapView.Selection.ClearSelection();
            await MapView.Editor.EditAsync(Layers.Selected, feature);
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Selection.ClearSelection();
        }

        private void OpenMenus(List<(string header, string desc, Func<Task> action, bool visiable)> menus, UIElement parent)
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
                            await (Window.GetWindow(this) as MainWindow).DoAsync(action);
                        }
                        catch (Exception ex)
                        {
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

        private void BtnMenuClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected;
            var features = MapView.Selection.SelectedFeatures.ToArray();
            List<(string header, string desc, Func<Task> action, bool visiable)> menus = new List<(string header, string desc, Func<Task> action, bool visiable)>()
           {
                ("合并","将多个图形合并为一个具有多个部分的图形",UnionAsync,
                (layer.GeometryType==GeometryType.Polygon || layer.GeometryType==GeometryType.Polyline)
                && features.Length>1),
                ("分离","将拥有多个部分的图形分离为单独的图形",
                SeparateAsync,(layer.GeometryType==GeometryType.Polygon || layer.GeometryType==GeometryType.Polyline)),
                ("连接","将折线的端点互相连接",LinkAsync,layer.GeometryType==GeometryType.Polyline
                && features.Length>1
                && features.All(p=>(p.Geometry as Polyline).Parts.Count==1)),
                ("反转","交换点的顺序",ReverseAsync,
                layer.GeometryType==GeometryType.Polyline||layer.GeometryType==GeometryType.Polygon),
                ("加密","在每两个折点之间添加更多的点",DensifyAsync,
                (layer.GeometryType==GeometryType.Polyline|| layer.GeometryType==GeometryType.Polygon)),
                ("简化","删除部分折点，降低图形的复杂度",SimplifyAsync,
                layer.GeometryType==GeometryType.Polyline|| layer.GeometryType==GeometryType.Polygon),
                ("建立副本","在原位置创建拥有相同图形和属性的要素",CreateCopyAsync, true),
            };
            OpenMenus(menus, sender as UIElement);

            async Task SeparateAsync()
            {
                var result = await FeatureUtility.SeparateAsync(layer, features);
                MapView.Selection.Select(result, true);
            }
            async Task UnionAsync()
            {
                var result = await FeatureUtility.UnionAsync(layer, features);
                MapView.Selection.Select(result, true);
            }

            async Task LinkAsync()
            {
                List<DialogItem> typeList = new List<DialogItem>();
                bool headToHead = false;
                bool reverse = false;
                bool auto = false;
                typeList.Add(new DialogItem("自动", "根据选择的顺序自动判断需要连接的点", () => auto = true));
                if (MapView.Selection.SelectedFeatures.Count == 2)
                {
                    typeList.Add(new DialogItem("起点相连", "起始点与起始点相连接", () => headToHead = true));
                    typeList.Add(new DialogItem("终点相连", "终结点与终结点相连接", () => headToHead = reverse = true));
                    typeList.Add(new DialogItem("头尾相连", "第一个要素的终结点与第二个要素的起始点相连接", null));
                    typeList.Add(new DialogItem("头尾相连（反转）", "第一个要素的起始点与第二个要素的终结点相连接", () => reverse = true));
                }
                else
                {
                    typeList.Add(new DialogItem("头尾相连", "每一个要素的终结点与前一个要素的起始点相连接", null));
                    typeList.Add(new DialogItem("头尾相连（反转）", "每一个要素的起始点与前一个要素的终结点相连接", () => reverse = true));
                }

                int result = await CommonDialog.ShowSelectItemDialogAsync(
                    "请选择连接类型", typeList, "查看图形起点和终点", async () =>
                     {
                         await MapView.Overlay.ShowHeadAndTailOfFeatures(features);
                         SnakeBar snake = new SnakeBar(Window.GetWindow(this))
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
                IReadOnlyList<Feature> result = await FeatureUtility.ReverseAsync(layer, features);
                MapView.Selection.Select(result, true);
            }

            async Task DensifyAsync()
            {
                double? num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大间隔（米）");
                if (num.HasValue)
                {
                    await FeatureUtility.DensifyAsync(layer, features, num.Value);
                }
            }
            async Task SimplifyAsync()
            {
                int i = await CommonDialog.ShowSelectItemDialogAsync("请选择简化方法", new DialogItem[]
                   {
                new DialogItem("间隔取点法","或每几个点保留一点"),
                new DialogItem("垂距法","中间隔一个点连接两个点，然后计算垂距或角度，在某一个值以内则可以删除中间间隔的点"),
                new DialogItem("分裂法","连接首尾点，计算每一点到连线的垂距，检查是否所有点距离小于限差；若不满足，则保留最大垂距的点，将直线一分为二，递归进行上述操作"),
                new DialogItem("最大偏离法","保证新的图形与旧图形之间的距离不超过一个值")
                   });
                double? num = null;
                switch (i)
                {
                    case 0:
                        num = await CommonDialog.ShowDoubleInputDialogAsync("请输入间隔几点取一点");

                        if (num.HasValue)
                        {
                            await FeatureUtility.IntervalTakePointsSimplifyAsync(layer, features, num.Value);
                        }
                        break;

                    case 1:
                        num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大垂距（米）");

                        if (num.HasValue)
                        {
                            await FeatureUtility.VerticalDistanceSimplifyAsync(layer, features, num.Value);
                        }
                        break;

                    case 2:
                        num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大垂距（米）");

                        if (num.HasValue)
                        {
                            await FeatureUtility.DouglasPeuckerSimplifyAsync(layer, features, num.Value);
                        }
                        break;

                    case 3:
                        num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大允许的偏移距离（米）");

                        if (num.HasValue)
                        {
                            await FeatureUtility.GeneralizeSimplifyAsync(layer, features, num.Value);
                        }
                        break;
                }
            }
            async Task CreateCopyAsync()
            {
                MapView.Selection.ClearSelection();
                var newFeatures = await FeatureUtility.CreateCopyAsync(layer, features);
                MapView.Selection.Select(newFeatures);
            }
        }

        private async Task ExportBase(FileFilterCollection filter, Func<string, Task> task)
        {
            string path = FileSystemDialog.GetSaveFile(filter, ensureExtension: true, defaultFileName: "图形");
            if (path != null)
            {
                await task(path);

                SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner)
                {
                    ShowButton = true,
                    ButtonContent = "打开"
                };
                snake.ButtonClick += (p1, p2) => IOUtility.OpenFileOrFolder(path);

                snake.ShowMessage("已导出到" + path);
            }
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

        private void BtnExportClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected;
            var features = MapView.Selection.SelectedFeatures.ToArray();
            List<(string header, string desc, Func<Task> action, bool visiable)> menus = new List<(string header, string desc, Func<Task> action, bool visiable)>()
           {
                ("导出到CSV表格","将图形导出为CSV表格",ToCsvAsync, true),
                ("导出到GeoJSON","将图形导出为GeoJSON",ToGeoJsonAsync, true),
            };
            OpenMenus(menus, sender as UIElement);

            Task ToCsvAsync()
            {
                return ExportBase(new FileFilterCollection().Add("Csv表格", "csv"), async path => await Csv.ExportAsync(path, MapView.Selection.SelectedFeatures));
            }
            Task ToGeoJsonAsync()
            {
                return ExportBase(new FileFilterCollection().Add("GeoJSON", "geojson"), async path => await GeoJson.ExportAsync(path, MapView.Selection.SelectedFeatures));
            }
        }
    }
}