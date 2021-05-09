using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using FzLib.Basic.Collection;
using FzLib.Extension;
using FzLib.UI.Dialog;
using MapBoard.Common.Resource;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
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

        private void SelectedFeaturesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            int count = MapView.Selection.SelectedFeatures.Count;
            btnRedraw.IsEnabled = count == 1;
            btnCut.IsEnabled = count == 1 &&
                (LayerCollection.Instance.Selected.Type == GeometryType.Polygon || LayerCollection.Instance.Selected.Type == GeometryType.Polyline);
            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count}个图形");
            if (LayerCollection.Instance.Selected.Table.GeometryType == GeometryType.Polyline)//线
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                sb.Append("，长度：" + Number.MeterToFitString(length));
            }
            else if (LayerCollection.Instance.Selected.Table.GeometryType == GeometryType.Polygon)//面
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                double area = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.AreaGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
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
                Attributes = FeatureAttributes.FromFeature(MapView.Selection.SelectedFeatures[0]);
            }
            else
            {
                Attributes = null;
            }
        }

        private FeatureAttributes attributes;

        public FeatureAttributes Attributes
        {
            get => attributes;
            set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

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

        public override double BarHeight { get; } = 48;

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            await MapView.Edit.DeleteSelectedFeatures();
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            await (Window.GetWindow(this) as MainWindow).Do(async () =>
            {
                SelectLayerDialog dialog = new SelectLayerDialog();
                if (dialog.ShowDialog() == true)
                {
                    LayerCollection.Instance.Selected.LayerVisible = false;
                    List<Feature> features = MapView.Selection.SelectedFeatures.ToList();
                    ShapefileFeatureTable targetTable = dialog.SelectedLayer.Table;
                    var newFeatures = features.Select(p => targetTable.CreateFeature(p.Attributes, p.Geometry));
                    await targetTable.AddFeaturesAsync(newFeatures);
                    await MapView.Selection.StopFrameSelect(false);
                    if (await CommonDialog.ShowYesNoDialogAsync("是否保留原图层中选中的图形？", "复制/移动") == false)
                    {
                        await LayerCollection.Instance.Selected.Table.DeleteFeaturesAsync(features);
                        LayerCollection.Instance.Selected.UpdateFeatureCount();
                    }
                    dialog.SelectedLayer.UpdateFeatureCount();
                    LayerCollection.Instance.Selected = dialog.SelectedLayer;
                }
            });
        }

        private void CutButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Edit.StartEdit(EditHelper.EditMode.Cut);
        }

        private void ReDrawButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Edit.StartEdit(EditHelper.EditMode.Draw);
        }

        private async void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            await MapView.Selection.StopFrameSelect(false);
        }

        private void BtnMenuClick(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            var layer = LayerCollection.Instance.Selected;

            ExtendedObservableCollection<Feature> features = ArcMapView.Instance.Selection.SelectedFeatures;
            List<(string header, Func<Task> action, bool visiable)> menus = new List<(string header, Func<Task> action, bool visiable)>()
           {
                ("合并",Union,(layer.Type==GeometryType.Polygon || layer.Type==GeometryType.Polyline)&& features.Count>1),
                ("连接",Link,layer.Type==GeometryType.Polyline&& features.Count>1),
                ("反转",Reverse,layer.Type==GeometryType.Polyline),
                ("加密",Densify,(layer.Type==GeometryType.Polyline|| layer.Type==GeometryType.Polygon)),
                ("简化",Simplify,(layer.Type==GeometryType.Polyline|| layer.Type==GeometryType.Polygon)&& features.Count==1),
                ("建立副本",CreateCopy, true),
                ("导出CSV表格",ToCsv, true),
            };

            foreach (var (header, action, visiable) in menus)
            {
                if (visiable)
                {
                    MenuItem item = new MenuItem() { Header = header };
                    item.Click += async (p1, p2) =>
                    {
                        try
                        {
                            await (Window.GetWindow(this) as MainWindow).Do(action);
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

            Task Union()
            {
                return FeatureUtility.Union(layer, features);
            }

            async Task Link()
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

                int result = await CommonDialog.ShowSelectItemDialogAsync("请选择连接类型", typeList);

                if (result < 0)
                {
                    return;
                }

                await FeatureUtility.Link(layer, features, headToHead, reverse);
            }

            Task Reverse()
            {
                return FeatureUtility.Reverse(layer, features);
            }

            async Task Densify()
            {
                double? num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大间隔（米）");
                if (num.HasValue)
                {
                    await FeatureUtility.Densify(layer, features, num.Value);
                }
            }
            async Task Simplify()
            {
                int i = await CommonDialog.ShowSelectItemDialogAsync("请选择简化方法", new DialogItem[]
                   {
                new DialogItem("间隔取点法","或每几个点保留一点"),
                new DialogItem("垂距法","中间隔一个点连接两个点，然后计算垂距或角度，在某一个值以内则可以删除中间间隔的点"),
                new DialogItem("分裂法","连接首尾点，计算每一点到连线的垂距，检查是否所有点距离小于限差；若不满足，则保留最大垂距的点，将直线一分为二，递归进行上述操作")
                   });
                double? num = null;
                switch (i)
                {
                    case 0:
                        num = await CommonDialog.ShowDoubleInputDialogAsync("请输入间隔几点取一点");

                        if (num.HasValue)
                        {
                            await FeatureUtility.IntervalTakePointsSimplify(layer, features, num.Value);
                        }
                        break;

                    case 1:
                        num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大垂距（米）");

                        if (num.HasValue)
                        {
                            await FeatureUtility.VerticalDistanceSimplify(layer, features, num.Value);
                        }
                        break;

                    case 2:
                        num = await CommonDialog.ShowDoubleInputDialogAsync("请输入最大垂距（米）");

                        if (num.HasValue)
                        {
                            await FeatureUtility.DouglasPeuckerSimplify(layer, features, num.Value);
                        }
                        break;
                }
            }
            Task CreateCopy()
            {
                return FeatureUtility.CreateCopy(layer, features);
            }
            async Task ToCsv()
            {
                string path = FileSystemDialog.GetSaveFile(new FileFilterCollection().Add("Csv表格", "csv"), ensureExtension: true, defaultFileName: "图形");
                if (path != null)
                {
                    await Csv.ExportAsync(ArcMapView.Instance.Selection.SelectedFeatures, path);

                    SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner);
                    snake.ShowButton = true;
                    snake.ButtonContent = "打开";
                    snake.ButtonClick += (p1, p2) => Process.Start(path);

                    snake.ShowMessage("已导出到" + path);
                }
            }
        }
    }
}