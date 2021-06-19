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
    public partial class SearchPanel : UserControlBase
    {
        public SearchPanel()
        {
            if (ExtensionUtility.PoiEngines.Count > 0)
            {
                SelectedPoiEngine = ExtensionUtility.PoiEngines[0];
            }
            InitializeComponent();
        }

        public void Initialize(ArcMapView mapView)
        {
            MapView = mapView;
        }

        public ArcMapView MapView { get; private set; }

        private int radius = 1000;

        private PoiInfo[] searchResult = Array.Empty<PoiInfo>();

        /// <summary>
        /// 关键次
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// 搜索半径
        /// </summary>
        public int Radius
        {
            get => radius;
            set
            {
                if (value < 100)
                {
                    radius = 100;
                }
                else if (value > 50000)
                {
                    radius = 50000;
                }
                radius = value;
                this.Notify(nameof(Radius));
            }
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        public PoiInfo[] SearchResult
        {
            get => searchResult;
            set => this.SetValueAndNotify(ref searchResult, value, nameof(SearchResult));
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
        public IPoiEngine SelectedPoiEngine { get; set; }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchResult = Array.Empty<PoiInfo>();
            MapView.Overlay.ClearPois();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPoiEngine == null)
            {
                await CommonDialog.ShowErrorDialogAsync("没有选择任何POI搜索引擎");
                return;
            }
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                await CommonDialog.ShowErrorDialogAsync("请输入关键词");
                return;
            }
            if ((sender as Button).IsEnabled == false)
            {
                return;
            }
            try
            {
                (sender as Button).IsEnabled = false;

                //周边搜索
                if (rbtnAround.IsChecked.Value)
                {
                    var point = GeometryEngine.Project(MapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).TargetGeometry, SpatialReferences.Wgs84) as MapPoint;

                    SearchResult = await SelectedPoiEngine.SearchAsync(Keyword, point, Radius);
                }
                //视图范围搜索
                else
                {
                    var rect = GeometryEngine.Project(MapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry).TargetGeometry, SpatialReferences.Wgs84) as Envelope;
                    SearchResult = await SelectedPoiEngine.SearchAsync(Keyword, rect);
                }

                //将搜索结果从GCJ02转为WGS84

                MapView.Overlay.ShowPois(SearchResult);
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
    }
}