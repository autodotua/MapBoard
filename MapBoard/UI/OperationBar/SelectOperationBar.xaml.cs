using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using FzLib.Basic.Collection;
using FzLib.Extension;
using FzLib.UI.Dialog;
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

namespace MapBoard.Main.UI.OperationBar
{
    /// <summary>
    /// EditOperationBar.xaml 的交互逻辑
    /// </summary>
    public partial class SelectOperationBar : OperationBarBase
    {
        public SelectOperationBar()
        {
            InitializeComponent();
            BoardTaskManager.BoardTaskChanged += BoardTaskChanged;
            Application.Current.MainWindow.SizeChanged += (p1, p2) => selectFeatureDialog?.ResetLocation();
            Application.Current.MainWindow.LocationChanged += (p1, p2) => selectFeatureDialog?.ResetLocation();
        }

        private SelectFeatureDialog selectFeatureDialog;
        protected override bool CanEdit => false;

        private void SelectedFeaturesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            int count = MapView.Selection.SelectedFeatures.Count;
            btnRedraw.IsEnabled = count == 1;
            btnMoreAttributes.IsEnabled = count == 1;
            LayerInfo layer = LayerCollection.Instance.Selected;
            btnCut.IsEnabled = layer.Table.GeometryType == GeometryType.Polygon
                || layer.Table.GeometryType == GeometryType.Polyline;
            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count}个图形");
            if (layer.Table.GeometryType == GeometryType.Polyline)//线
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p =>
                GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                sb.Append("，长度：" + Number.MeterToFitString(length));
            }
            else if (layer.Table.GeometryType == GeometryType.Polygon)//面
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p =>
                GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                double area = MapView.Selection.SelectedFeatures.Sum(p =>
                GeometryEngine.AreaGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                sb.Append("，周长：" + Number.MeterToFitString(length));
                sb.Append("，面积：" + Number.SquareMeterToFitString(area));
            }
            Message = sb.ToString();

            if (count > 1 && (selectFeatureDialog == null || selectFeatureDialog.IsClosed))
            {
                var mainWindow = Application.Current.MainWindow;
                selectFeatureDialog = new SelectFeatureDialog();
                selectFeatureDialog.Show();
            }

            if (count == 1)
            {
                attributes = FeatureAttributes.FromFeature(layer, MapView.Selection.SelectedFeatures[0]);
            }
            else
            {
                attributes = null;
            }
            this.Notify(nameof(Attributes));
        }

        private FeatureAttributes attributes;

        public override FeatureAttributes Attributes => attributes;

        private void BoardTaskChanged(object sender, BoardTaskManager.BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTaskManager.BoardTask.Select)
            {
                MapView.Selection.SelectedFeatures.CollectionChanged += SelectedFeaturesChanged;
                SelectedFeaturesChanged(null, null);
                Show();
            }
            else
            {
                MapView.Selection.SelectedFeatures.CollectionChanged -= SelectedFeaturesChanged;
                Hide();
            }
        }

        public ArcMapView MapView => ArcMapView.Instance;

        private string message = "正在编辑";

        public string Message
        {
            get => message;
            set
            {
                message = value;
                Notify(nameof(Message));
            }
        }

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
            {
                await FeatureUtility.DeleteAsync(LayerCollection.Instance.Selected, ArcMapView.Instance.Selection.SelectedFeatures.ToArray());
                ArcMapView.Instance.Selection.ClearSelection();
            }, true);
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
            {
                SelectLayerDialog dialog = new SelectLayerDialog();
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    bool copy = await CommonDialog.ShowYesNoDialogAsync("是否保留原图层中选中的图形？");

                    await FeatureUtility.CopyOrMoveAsync(LayerCollection.Instance.Selected, dialog.SelectedLayer, MapView.Selection.SelectedFeatures.ToArray(), copy);
                    LayerCollection.Instance.Selected.LayerVisible = false;
                    dialog.SelectedLayer.LayerVisible = false;
                    MapView.Selection.ClearSelection();
                    LayerCollection.Instance.Selected = dialog.SelectedLayer;
                }
            });
        }

        private async void CutButtonClick(object sender, RoutedEventArgs e)
        {
            var features = ArcMapView.Instance.Selection.SelectedFeatures.ToArray();
            var line = await MapView.Editor.GetPolylineAsync();
            if (line != null)
            {
                await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
                {
                    await FeatureUtility.CutAsync(LayerCollection.Instance.Selected, features, line);
                }, true);
            }
        }

        private async void EditButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(MapView.Selection.SelectedFeatures.Count == 1);
            await MapView.Editor.EditAsync(LayerCollection.Instance.Selected, MapView.Selection.SelectedFeatures[0]);
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Selection.ClearSelection();
        }

        private void BtnMenuClick(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            var layer = LayerCollection.Instance.Selected;

            var features = ArcMapView.Instance.Selection.SelectedFeatures.ToArray();
            List<(string header, string desc, Func<Task> action, bool visiable)> menus = new List<(string header, string desc, Func<Task> action, bool visiable)>()
           {
                ("合并","将多个图形合并为一个具有多个部分的图形",UnionAsync,
                (layer.Table.GeometryType==GeometryType.Polygon || layer.Table.GeometryType==GeometryType.Polyline)
                && features.Length>1),
                ("分离","将拥有多个部分的图形分离为单独的图形",
                SeparateAsync,(layer.Table.GeometryType==GeometryType.Polygon || layer.Table.GeometryType==GeometryType.Polyline)),
                ("连接","将折线的端点互相连接",LinkAsync,layer.Table.GeometryType==GeometryType.Polyline
                && features.Length>1
                && features.All(p=>(p.Geometry as Polyline).Parts.Count==1)),
                ("反转","交换点的顺序",ReverseAsync,
                layer.Table.GeometryType==GeometryType.Polyline||layer.Table.GeometryType==GeometryType.Polygon),
                ("加密","在每两个折点之间添加更多的点",DensifyAsync,
                (layer.Table.GeometryType==GeometryType.Polyline|| layer.Table.GeometryType==GeometryType.Polygon)),
                ("简化","删除部分折点，降低图形的复杂度",SimplifyAsync,
                layer.Table.GeometryType==GeometryType.Polyline|| layer.Table.GeometryType==GeometryType.Polygon),
                ("建立副本","在原位置创建拥有相同图形和属性的要素",CreateCopyAsync, true),
                ("导出CSV表格","将图形导出为CSV表格",ToCsvAsync, true),
            };

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

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;

            Task SeparateAsync()
            {
                return FeatureUtility.SeparateAsync(layer, features);
            }
            Task UnionAsync()
            {
                return FeatureUtility.UnionAsync(layer, features);
            }

            async Task LinkAsync()
            {
                List<DialogItem> typeList = new List<DialogItem>();
                bool headToHead = false;
                bool reverse = false;
                if (ArcMapView.Instance.Selection.SelectedFeatures.Count == 2)
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
                         await ArcMapView.Instance.Overlay.ShowHeadAndTailOfFeatures(features);
                         SnakeBar snake = new SnakeBar(Window.GetWindow(this))
                         {
                             ShowButton = true,
                             ButtonContent = "继续",
                             Duration = Duration.Forever
                         };
                         snake.ButtonClick += async (p1, p2) =>
                          {
                              ArcMapView.Instance.Overlay.ClearHeadAndTail();
                              snake.Hide();
                              await LinkAsync();
                          };

                         snake.ShowMessage("正在显示图形起点和终点");
                     });

                if (result < 0)
                {
                    return;
                }

                await FeatureUtility.LinkAsync(layer, features, headToHead, reverse);
            }

            Task ReverseAsync()
            {
                return FeatureUtility.ReverseAsync(layer, features);
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
            Task CreateCopyAsync()
            {
                return FeatureUtility.CreateCopyAsync(layer, features);
            }
            async Task ToCsvAsync()
            {
                string path = FileSystemDialog.GetSaveFile(new FileFilterCollection().Add("Csv表格", "csv"), ensureExtension: true, defaultFileName: "图形");
                if (path != null)
                {
                    await Csv.ExportAsync(ArcMapView.Instance.Selection.SelectedFeatures, path);

                    SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner)
                    {
                        ShowButton = true,
                        ButtonContent = "打开"
                    };
                    snake.ButtonClick += (p1, p2) => Process.Start(path);

                    snake.ShowMessage("已导出到" + path);
                }
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
    }
}