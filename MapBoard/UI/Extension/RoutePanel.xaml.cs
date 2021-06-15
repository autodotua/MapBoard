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

        public Location Origin { get; } = new Location();
        public Location Destination { get; } = new Location();

        /// <summary>
        /// 搜索结果
        /// </summary>
        public RouteInfo[] SearchResult
        {
            get => searchResult;
            set => this.SetValueAndNotify(ref searchResult, value, nameof(SearchResult));
        }

        /// <summary>
        /// 使用的搜索引擎
        /// </summary>
        public IRouteEngine SelectedRouteEngine { get; set; }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchResult = Array.Empty<RouteInfo>();
            MapView.Overlay.ClearPois();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRouteEngine == null)
            {
                await CommonDialog.ShowErrorDialogAsync("没有选择任何POI搜索引擎");
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

        private async void ChoosePointButton_Click(object sender, RoutedEventArgs e)
        {
            var point = await MapView.Editor.GetPointAsync();
            if (point != null)
            {
                point = GeometryEngine.Project(point, SpatialReferences.Wgs84) as MapPoint;
                Location l = ((sender as FrameworkElement).Tag as Location);
                l.Longitude = point.X;
                l.Latitude = point.Y;
                if (l == Origin)
                {
                    this.Notify(nameof(Origin));
                    MapView.Overlay.SetRouteOrigin(point);
                }
                else
                {
                    this.Notify(nameof(Destination));
                    MapView.Overlay.SetRouteDestination(point);
                }
            }
        }
    }

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