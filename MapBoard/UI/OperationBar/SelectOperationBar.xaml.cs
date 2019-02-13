using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using FzLib.Control.Dialog;
using MapBoard.Main.Style;
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
        }

        private void SelectedFeaturesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            btnRedraw.IsEnabled = MapView.Selection.SelectedFeatures.Count == 1;
            btnCut.IsEnabled = MapView.Selection.SelectedFeatures.Count == 1 &&
                (StyleCollection.Instance.Selected.Type == GeometryType.Polygon || StyleCollection.Instance.Selected.Type == GeometryType.Polyline);
            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count.ToString()}个图形");
            if (StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polyline)//线
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                sb.Append("，长度：" + Number.MeterToFitString(length));
            }
            else if (StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polygon)//面
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                double area = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.AreaGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                sb.Append("，周长：" + Number.MeterToFitString(length));
                sb.Append("，面积：" + Number.SquareMeterToFitString(area));
            }
            Message = sb.ToString();
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

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            await MapView.Edit.DeleteSelectedFeatures();
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            SelectStyleDialog dialog = new SelectStyleDialog();
            if (dialog.ShowDialog() == true)
            {
                StyleCollection.Instance.Selected.LayerVisible = false;
                ObservableCollection<Feature> features = MapView.Selection.SelectedFeatures;
                ShapefileFeatureTable targetTable = dialog.SelectedStyle.Table;
                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                await MapView.Selection.StopFrameSelect(false);
                dialog.SelectedStyle.UpdateFeatureCount();
                StyleCollection.Instance.Selected = dialog.SelectedStyle;
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

            var style = StyleCollection.Instance.Selected;

            List<(string header, Action action, bool visiable)> menus = new List<(string header, Action action, bool visiable)>()
           {
                ("合并",Union,(style.Type==GeometryType.Polygon || style.Type==GeometryType.Polyline)&& ArcMapView.Instance.Selection.SelectedFeatures.Count>1),
                ("连接",Link,style.Type==GeometryType.Polyline&& ArcMapView.Instance.Selection.SelectedFeatures.Count>1),
                //("反转",Reverse,style.Type==GeometryType.Polyline&& ArcMapView.Instance.Selection.SelectedFeatures.Count==1),
 };



            foreach (var (header, action, visiable) in menus)
            {
                if (visiable)
                {
                    MenuItem item = new MenuItem() { Header = header };
                    item.Click += (p1, p2) => action();
                    menu.Items.Add(item);
                }
            }

            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;

            async void Union()
            {
                Geometry geometry = GeometryEngine.Union(ArcMapView.Instance.Selection.SelectedFeatures.Select(p => p.Geometry));
                var firstFeature = ArcMapView.Instance.Selection.SelectedFeatures[0];
                firstFeature.Geometry = geometry;
                await style.Table.UpdateFeatureAsync(firstFeature);
                await style.Table.DeleteFeaturesAsync(ArcMapView.Instance.Selection.SelectedFeatures.Where(p => p != firstFeature));
                ArcMapView.Instance.Selection.ClearSelection();
            }

            async void Link()
            {
                var features = ArcMapView.Instance.Selection.SelectedFeatures.ToArray();
                List<(string, string, Action)> typeList = new List<(string, string, Action)>();
                int type = 0;
                if (ArcMapView.Instance.Selection.SelectedFeatures.Count == 2)
                {
                    typeList.Add(("尾1头——头2尾", "起始点与起始点相连接", () => type = 1));
                    typeList.Add(("头1尾——尾2头", "终结点与终结点相连接", () => type = 2));
                    typeList.Add(("头1尾——头2尾", "第一个要素的终结点与第二个要素的起始点相连接", () => type = 3));
                    typeList.Add(("头2尾——头1尾", "第一个要素的起始点与第二个要素的终结点相连接", () => type = 4));
                }
                else
                {
                    typeList.Add(("头n尾——头n+1尾", "每一个要素的终结点与前一个要素的起始点相连接", () => type = 5));
                    typeList.Add(("头n尾——头n-1尾", "每一个要素的起始点与前一个要素的终结点相连接", () => type = 6));
                }

                TaskDialog.ShowWithCommandLinks("连接类型", "请选择连接类型", typeList, cancelable: true);

                if (type == 0)
                {
                    return;
                }
                List<MapPoint> points = null;

                if (type <= 4)
                {
                    List<MapPoint> points1 = GetPoints(features[0]);
                    List<MapPoint> points2 = GetPoints(features[1]);
                    switch (type)
                    {
                        case 1:
                            points1.Reverse();
                            points1.AddRange(points2);
                            break;
                        case 2:
                            points2.Reverse();
                            points1.AddRange(points2);
                            break;
                        case 3:
                            points1.AddRange(points2);
                            break;
                        case 4:
                            points1.InsertRange(0, points2);
                            break;
                    }
                    points = points1;

                }
                else
                {
                    IEnumerable<List<MapPoint>> pointsGroup = features.Select(p => GetPoints(p));
                    if (type == 6)
                    {
                        pointsGroup = pointsGroup.Reverse();
                    }
                    points = new List<MapPoint>();
                    foreach (var part in pointsGroup)
                    {
                        points.AddRange(part);
                    }
                }
                features[0].Geometry = new Polyline(points);

                await style.Table.UpdateFeatureAsync(features[0]);

                await style.Table.DeleteFeaturesAsync(features.Where(p => p != features[0]));
                ArcMapView.Instance.Selection.ClearSelection();

            }

            async void Reverse()
            {
                Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];

                Polyline line = feature.Geometry as Polyline;
                List<MapPoint> points = GetPoints(feature);
                points.Reverse();
                feature.Geometry = new Polyline(points);
                await style.Table.UpdateFeatureAsync(feature);
                ArcMapView.Instance.Selection.ClearSelection();

            }

            List<MapPoint> GetPoints(Feature feature)
            {
                Polyline line = feature.Geometry as Polyline;
                List<MapPoint> points = new List<MapPoint>();
                foreach (var part in line.Parts)
                {
                    points.AddRange(part.Points);
                }
                return points;
            }

        }
    }
}
