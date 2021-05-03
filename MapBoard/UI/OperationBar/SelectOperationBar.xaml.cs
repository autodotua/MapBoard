using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using FzLib.Basic.Collection;
using FzLib.UI.Dialog;
using MapBoard.Main.IO;
using MapBoard.Main.Layer;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static MapBoard.Main.Util.FeatureUtility;
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
            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count.ToString()}个图形");
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
            //else if(count==1 && selectFeatureDialog!=null && !selectFeatureDialog.IsClosed)
            //{
            //    selectFeatureDialog?.Close();
            //}
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

        public override double BarHeight { get; } = 24;

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            await MapView.Edit.DeleteSelectedFeatures();
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            SelectLayerDialog dialog = new SelectLayerDialog();
            if (dialog.ShowDialog() == true)
            {
                LayerCollection.Instance.Selected.LayerVisible = false;
                ObservableCollection<Feature> features = MapView.Selection.SelectedFeatures;
                ShapefileFeatureTable targetTable = dialog.SelectedStyle.Table;
                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                await MapView.Selection.StopFrameSelect(false);
                dialog.SelectedStyle.UpdateFeatureCount();
                LayerCollection.Instance.Selected = dialog.SelectedStyle;
            }
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

            var style = LayerCollection.Instance.Selected;

            List<(string header, Action<LayerInfo> action, bool visiable)> menus = new List<(string header, Action<LayerInfo> action, bool visiable)>()
           {
                ("合并",Union,(style.Type==GeometryType.Polygon || style.Type==GeometryType.Polyline)&& ArcMapView.Instance.Selection.SelectedFeatures.Count>1),
                ("连接",Link,style.Type==GeometryType.Polyline&& ArcMapView.Instance.Selection.SelectedFeatures.Count>1),
                ("反转",Reverse,style.Type==GeometryType.Polyline&& ArcMapView.Instance.Selection.SelectedFeatures.Count==1),
                ("加密",Densify,(style.Type==GeometryType.Polyline||style.Type==GeometryType.Polygon)&& ArcMapView.Instance.Selection.SelectedFeatures.Count==1),
                ("删除部分点",RemoveSomePoints,(style.Type==GeometryType.Polyline||style.Type==GeometryType.Polygon)&& ArcMapView.Instance.Selection.SelectedFeatures.Count==1),

                ("建立副本",CreateCopy, true),
                ("导出CSV表格",ToCsv, true),
            };

            foreach (var (header, action, visiable) in menus)
            {
                if (visiable)
                {
                    MenuItem item = new MenuItem() { Header = header };
                    item.Click += (p1, p2) => action(style);
                    menu.Items.Add(item);
                }
            }

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;
        }
    }
}