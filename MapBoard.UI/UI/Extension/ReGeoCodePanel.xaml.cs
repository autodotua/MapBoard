using System;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Extension;
using MapBoard.Extension;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using MapBoard.Mapping;
using static FzLib.Extension.ExtendedINotifyPropertyChanged;
using System.ComponentModel;
using ModernWpf.FzExtension.CommonDialog;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Util;

namespace MapBoard.UI.Extension
{
    /// <summary>
    /// SearchPanel.xaml 的交互逻辑
    /// </summary>
    public partial class ReGeoCodePanel : UserControlBase
    {
        public ReGeoCodePanel()
        {
            if (ExtensionUtility.ReGeoCodeEngines.Count > 0)
            {
                SelectedReGeoCodeEngine = ExtensionUtility.ReGeoCodeEngines[0];
            }
            InitializeComponent();
        }

        public void Initialize(ArcMapView mapView)
        {
            MapView = mapView;
        }

        public ArcMapView MapView { get; private set; }

        private int radius = 1000;

        private LocationInfo searchResult = null;

        /// <summary>
        /// 搜索半径
        /// </summary>
        public int Radius
        {
            get => radius;
            set
            {
                if (value < 1)
                {
                    radius = 1;
                }
                else if (value > 3000)
                {
                    radius = 3000;
                }
                radius = value;
                this.Notify(nameof(Radius));
            }
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        public LocationInfo SearchResult
        {
            get => searchResult;
            set => this.SetValueAndNotify(ref searchResult, value, nameof(SearchResult));
        }

        private Location point;

        public Location Point
        {
            get => point;
            private set => this.SetValueAndNotify(ref point, value, nameof(Point));
        }

        private PoiInfo selectedPoi;

        /// <summary>
        /// 选中的POI
        /// </summary>
        public PoiInfo SelectedPoi
        {
            get => selectedPoi;
            set
            {
                this.SetValueAndNotify(ref selectedPoi, value, nameof(SelectedPoi));
                MapView.Overlay.SelectPoi(value);
            }
        }

        /// <summary>
        /// 使用的搜索引擎
        /// </summary>
        public IReGeoCodeEngine SelectedReGeoCodeEngine { get; set; }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedReGeoCodeEngine == null)
            {
                await CommonDialog.ShowErrorDialogAsync("没有选择任何POI搜索引擎");
                return;
            }
            if (Point == null || Point.Longitude * Point.Latitude == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("请先设置点的经纬度");
                return;
            }
            if ((sender as Button).IsEnabled == false)
            {
                return;
            }
            try
            {
                (sender as Button).IsEnabled = false;

                SearchResult = await SelectedReGeoCodeEngine.SearchAsync(Point.ToMapPoint(), Radius);
                MapView.Overlay.ShowPois(SearchResult.Pois);
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

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private async void ChoosePointButton_Click(object sender, RoutedEventArgs e)
        {
            var point = await MapView.Editor.GetPointAsync();
            if (point != null)
            {
                point = GeometryEngine.Project(point, SpatialReferences.Wgs84) as MapPoint;
                Point = point.ToLocation();
                MapView.Overlay.ShowLocation(point);
            }
        }

        private void LocationTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Point == null)
            {
                Point = new Location();
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            MapView.Overlay.ClearLocation();
            MapView.Overlay.ClearPois();
            SearchResult = null;
        }
    }
}