using MapBoard.Common;
using System;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Extension;
using MapBoard.Extension;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using MapBoard.Main.UI.Map;
using static FzLib.Extension.ExtendedINotifyPropertyChanged;
using System.ComponentModel;
using ModernWpf.FzExtension.CommonDialog;
using System.Windows.Controls;
using MapBoard.Main.Util;
using Esri.ArcGISRuntime.Mapping;
using System.Windows.Data;
using System.Globalization;
using MapBoard.Main.UI.Dialog;
using System.Diagnostics;
using Esri.ArcGISRuntime.Data;
using System.Collections.Generic;
using System.Linq;

namespace MapBoard.Main.UI.Extension
{
    /// <summary>
    /// SearchPanel.xaml 的交互逻辑
    /// </summary>
    public partial class RoutePanel : UserControlBase, INotifyPropertyChanged
    {
        public RoutePanel()
        {
            if (ExtensionUtility.RouteEngines.Count > 0)
            {
                SelectedRouteEngine = ExtensionUtility.RouteEngines[0];
            }
            InitializeComponent();
        }

        public void Initialize(ArcMapView mapView)
        {
            MapView = mapView;
        }

        public ArcMapView MapView { get; private set; }

        private RouteInfo[] searchResult = Array.Empty<RouteInfo>();

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 起点
        /// </summary>
        private Location origin;

        public Location Origin
        {
            get => origin;
            private set => this.SetValueAndNotify(ref origin, value, nameof(Origin));
        }

        /// <summary>
        /// 终点
        /// </summary>
        private Location destination;

        public Location Destination
        {
            get => destination;
            private set => this.SetValueAndNotify(ref destination, value, nameof(Destination));
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        public RouteInfo[] SearchResult
        {
            get => searchResult;
            set => this.SetValueAndNotify(ref searchResult, value, nameof(SearchResult));
        }

        private RouteInfo selectedRoute;

        /// <summary>
        /// 选中的某一条路线
        /// </summary>
        public RouteInfo SelectedRoute
        {
            get => selectedRoute;
            set
            {
                this.SetValueAndNotify(ref selectedRoute, value, nameof(SelectedRoute));
                MapView.Overlay.SelectRoute(value);
            }
        }

        private RouteStepInfo selectedStep;

        /// <summary>
        /// 选中的某一步骤
        /// </summary>
        public RouteStepInfo SelectedStep
        {
            get => selectedStep;
            set
            {
                this.SetValueAndNotify(ref selectedStep, value, nameof(SelectedStep));
                MapView.Overlay.SelectStep(value);
            }
        }

        /// <summary>
        /// 使用的搜索引擎
        /// </summary>
        public IRouteEngine SelectedRouteEngine { get; set; }

        /// <summary>
        /// 点击清空按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchResult = Array.Empty<RouteInfo>();
            MapView.Overlay.ClearRoutes();
        }

        /// <summary>
        /// 点击搜索按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRouteEngine == null)
            {
                await CommonDialog.ShowErrorDialogAsync("没有选择任何POI搜索引擎");
                return;
            }
            if (Origin == null || Destination == null || Origin.Longitude * Origin.Latitude * Destination.Longitude * Destination.Latitude == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("请先完全设置起点和终点的经纬度");
                return;
            }
            if ((sender as Button).IsEnabled == false)
            {
                return;
            }
            try
            {
                (sender as Button).IsEnabled = false;
                var type = (RouteType)(cbbType.SelectedIndex + 1);

                SearchResult = await SelectedRouteEngine.SearchRouteAsync(type, Origin.ToMapPoint(), Destination.ToMapPoint());

                //将搜索结果从GCJ02转为WGS84

                MapView.Overlay.ShowRoutes(SearchResult);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "搜索失败");
            }
            finally
            {
                (sender as Button).IsEnabled = true;
            }
        }

        /// <summary>
        /// 点击选择起点和终点按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ChoosePointButton_Click(object sender, RoutedEventArgs e)
        {
            var point = await MapView.Editor.GetPointAsync();
            if (point != null)
            {
                point = GeometryEngine.Project(point, SpatialReferences.Wgs84) as MapPoint;
                if ((sender as FrameworkElement).Tag.Equals("1"))
                {
                    Origin = point.ToLocation();
                    MapView.Overlay.SetRouteOrigin(point);
                }
                else
                {
                    Destination = point.ToLocation();
                    MapView.Overlay.SetRouteDestination(point);
                }
            }
        }

        /// <summary>
        /// 详情面板关闭时，取消选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Flyout_Closed(object sender, object e)
        {
            MapView.Overlay.SelectStep(null);
        }

        /// <summary>
        /// 点击导入按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectLayerDialog(MapView.Layers, new[] { GeometryType.Point, GeometryType.Multipoint, GeometryType.Polyline }, false);
            if (await dialog.ShowAsync() == ModernWpf.Controls.ContentDialogResult.Primary && dialog.SelectedLayer != null)
            {
                var layer = dialog.SelectedLayer;
                var route = SelectedRoute;
                List<Feature> newFeatures = new List<Feature>();
                Debug.Assert(SelectedRoute != null);
                switch (layer.GeometryType)
                {
                    case GeometryType.Point:
                        {
                            bool first = true;
                            foreach (var step in route.Steps)
                            {
                                if (first)
                                {
                                    first = false;
                                    var f = layer.CreateFeature();
                                    f.Geometry = step.Locations[0].ToMapPoint();
                                    newFeatures.Add(f);
                                }
                                foreach (var p in step.Locations.Skip(1))
                                {
                                    var f = layer.CreateFeature();
                                    f.Geometry = p.ToMapPoint();
                                    newFeatures.Add(f);
                                }
                            }
                        }
                        break;

                    case GeometryType.Polyline:
                        {
                            List<IEnumerable<MapPoint>> pointsCollection = new List<IEnumerable<MapPoint>>();
                            foreach (var step in route.Steps)
                            {
                                pointsCollection.Add(step.Locations.Select(p => p.ToMapPoint()));
                            }
                            var f = layer.CreateFeature();
                            f.Geometry = new Polyline(pointsCollection);
                            newFeatures.Add(f);
                        }
                        break;

                    case GeometryType.Polygon:
                        await CommonDialog.ShowErrorDialogAsync("不支持导入到多边形图层");
                        return;

                    case GeometryType.Multipoint:
                        {
                            foreach (var step in route.Steps)
                            {
                                List<MapPoint> points = new List<MapPoint>();
                                var f = layer.CreateFeature();
                                f.Geometry = new Multipoint(step.Locations.Select(p => p.ToMapPoint()));
                                newFeatures.Add(f);
                            }
                        }
                        break;

                    default:
                        throw new NotSupportedException();
                }

                if (newFeatures.Count > 0)
                {
                    await layer.AddFeaturesAsync(newFeatures, Map.Model.FeaturesChangedSource.Import);
                }
                MapView.Layers.Selected = layer;
                if (!layer.LayerVisible)
                {
                    layer.LayerVisible = true;
                }
            }
        }

        private void LocationTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Origin == null)
            {
                Origin = new Location();
            }
            if (Destination == null)
            {
                Destination = new Location();
            }
        }
    }

    /// <summary>
    /// 为ListViewItem提供显示索引支持
    /// </summary>
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            ListViewItem item = (ListViewItem)value;
            ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
            int index = listView.ItemContainerGenerator.IndexFromContainer(item);
            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}