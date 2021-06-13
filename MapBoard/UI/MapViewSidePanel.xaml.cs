using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Common.Model;
using MapBoard.Extension;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MapBoard.Main.UI
{
    /// <summary>
    /// MapViewInfoPanel.xaml 的交互逻辑
    /// </summary>
    public partial class MapViewSidePanel : UserControlBase
    {
        private object lockObj = new object();
        private Action setScaleAction;
        private Timer timer;
        private Action updateScaleAndPositionAction;

        public MapViewSidePanel()
        {
            if (PoiUtility.PoiEngines.Count > 0)
            {
                SelectedPoiEngine = PoiUtility.PoiEngines[0];
            }
            InitializeComponent();
            Config.Instance.PropertyChanged += Config_PropertyChanged;
            BaseLayers = Config.Instance.BaseLayers;
            //用于限制最多100毫秒更新一次
            timer = new Timer(new TimerCallback(p =>
             {
                 lock (lockObj)
                 {
                     if (updateScaleAndPositionAction != null)
                     {
                         Dispatcher.Invoke(updateScaleAndPositionAction);
                         updateScaleAndPositionAction = null;
                     }
                     if (setScaleAction != null)
                     {
                         Dispatcher.Invoke(setScaleAction);
                         setScaleAction = null;
                     }
                 }
             }), null, 100, 100);
        }

        public ArcMapView MapView { get; private set; }

        public void Initialize(ArcMapView mapView)
        {
            MapView = mapView;
            mapView.ViewpointChanged += MapView_ViewpointChanged;
            mapView.PreviewMouseMove += MapView_PreviewMouseMove;
        }

        private void Config_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.BaseLayers))
            {
                BaseLayers = Config.Instance.BaseLayers;
            }
        }

        #region 右下角坐标和比例尺

        private string latitude;
        private MapPoint location;

        private string longitude;

        private string scale;

        public string Latitude
        {
            get => latitude;
            private set => this.SetValueAndNotify(ref latitude, value, nameof(Latitude));
        }

        public string Longitude
        {
            get => longitude;
            private set => this.SetValueAndNotify(ref longitude, value, nameof(Longitude));
        }

        public string Scale
        {
            get => scale;
            private set => this.SetValueAndNotify(ref scale, value, nameof(Scale));
        }

        public void UpdateScaleAndPosition(Point? position = null)
        {
            updateScaleAndPositionAction = () =>
            {
                if (position.HasValue)
                {
                    location = MapView.ScreenToLocation(position.Value);
                }
                if (location != null)
                {
                    location = GeometryEngine.Project(location, SpatialReferences.Wgs84) as MapPoint;
                    Latitude = location.Y.ToString("0.000000");
                    Longitude = location.X.ToString("0.000000");
                    Scale = (MapView.UnitsPerPixel * ActualWidth * Math.Cos(Math.PI / 180 * location.Y)).ToString("0.00m");
                }
                var level = 5 * (20 + Math.Log(MapView.Map.MaxScale / MapView.MapScale, 2));
                if (level < 0)
                {
                    level = 0;
                }
                ScaleLevel = level.ToString("0") + "%";

                if (!sldScale.IsMouseOver)
                {
                    mapScalePercent = level;
                    this.Notify(nameof(MapScalePercent));
                }
            };
        }

        private void MapView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (IsLoaded && MapView.IsLoaded && MapView.IsMouseOver)
            {
                UpdateScaleAndPosition(e.GetPosition(MapView));
            }
        }

        private void MapView_ViewpointChanged(object sender, EventArgs e)
        {
            (vbxRotate.RenderTransform as RotateTransform).Angle = -MapView.MapRotation;
            if (IsLoaded && MapView.IsLoaded)
            {
                UpdateScaleAndPosition();
            }
        }

        #endregion 右下角坐标和比例尺

        #region 缩放按钮和缩放条

        private double mapScalePercent = 0;
        private string scaleLevel;

        public double MapScalePercent
        {
            get => mapScalePercent;

            set
            {
                if (mapScalePercent is < 0 or > 100)
                {
                    throw new ArgumentOutOfRangeException();
                }
                mapScalePercent = value;
                setScaleAction = () => MapView.SetViewpointScaleAsync(MapView.Map.MaxScale / Math.Pow(2, value / 5 - 20));
            }
        }

        public string ScaleLevel
        {
            get => scaleLevel;
            private set => this.SetValueAndNotify(ref scaleLevel, value, nameof(ScaleLevel));
        }

        private void PanelScale_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseScalePanel();
        }

        private void CloseScalePanel()
        {
            DoubleAnimation aniHeight = new DoubleAnimation(36, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniTbkOpacity = new DoubleAnimation(1, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniSliderOpacity = new DoubleAnimation(0, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            bdScale.BeginAnimation(HeightProperty, aniHeight);
            tbkScale.BeginAnimation(OpacityProperty, aniTbkOpacity);
            grdScale.BeginAnimation(OpacityProperty, aniSliderOpacity);
        }

        private void OpenScalePanel()
        {
            DoubleAnimation aniHeight = new DoubleAnimation(240, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniTbkOpacity = new DoubleAnimation(0, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniSliderOpacity = new DoubleAnimation(1, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            bdScale.BeginAnimation(HeightProperty, aniHeight);
            tbkScale.BeginAnimation(OpacityProperty, aniTbkOpacity);
            grdScale.BeginAnimation(OpacityProperty, aniSliderOpacity);
        }

        private void ScaleButton_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenScalePanel();
        }

        #endregion 缩放按钮和缩放条

        #region 底图

        private List<BaseLayerInfo> baseLayers;

        private bool isLayerPanelOpened = false;

        public List<BaseLayerInfo> BaseLayers
        {
            get => baseLayers;
            set => this.SetValueAndNotify(ref baseLayers, value, nameof(BaseLayers));
        }

        private void LayersPanel_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isLayerPanelOpened && !IsMouseOver)
            {
                CloseLayersPanel();
            }
        }

        private void LayersPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isLayerPanelOpened)
            {
                OpenLayersPanel();
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var baseLayer = (sender as FrameworkElement).Tag as BaseLayerInfo;
            var arcBaseLayer = MapView.Map.Basemap.BaseLayers.FirstOrDefault(p => p.Id == baseLayer.TempID.ToString());
            if (arcBaseLayer != null)
            {
                arcBaseLayer.IsVisible = baseLayer.Enable;
            }
        }

        private void CloseLayerPanelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseLayersPanel();
        }

        private void CloseLayersPanel()
        {
            isLayerPanelOpened = false;
            DoubleAnimation aniWidth = new DoubleAnimation(36, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniHeight = new DoubleAnimation(36, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniIconOpacity = new DoubleAnimation(1, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniDgOpacity = new DoubleAnimation(0, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            aniDgOpacity.Completed += (p1, p2) =>
            {
                grdLayers.Visibility = Visibility.Collapsed;
            };
            bdLayers.BeginAnimation(WidthProperty, aniWidth);
            bdLayers.BeginAnimation(HeightProperty, aniHeight);
            iconLayers.BeginAnimation(OpacityProperty, aniIconOpacity);
            grdLayers.BeginAnimation(OpacityProperty, aniDgOpacity);
            bdLayers.Cursor = Cursors.Hand;
        }

        private void LayersDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Column.DisplayIndex == 1)
                {
                }
            }
        }

        private void OpenLayersPanel()
        {
            isLayerPanelOpened = true;
            DoubleAnimation aniWidth = new DoubleAnimation(360, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniHeight = new DoubleAnimation(360, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniIconOpacity = new DoubleAnimation(0, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniDgOpacity = new DoubleAnimation(1, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            bdLayers.BeginAnimation(WidthProperty, aniWidth);
            bdLayers.BeginAnimation(HeightProperty, aniHeight);
            iconLayers.BeginAnimation(OpacityProperty, aniIconOpacity);
            grdLayers.Visibility = Visibility.Visible;
            grdLayers.BeginAnimation(OpacityProperty, aniDgOpacity);
            bdLayers.Cursor = Cursors.Arrow;
            dgLayers.Focus();
        }

        private void OpenSettingDialogButton_Click(object sender, RoutedEventArgs e)
        {
            CloseLayersPanel();
            new SettingDialog(MapView) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var baseLayer = (sender as FrameworkElement).Tag as BaseLayerInfo;
            var arcBaseLayer = MapView.Map.Basemap.BaseLayers.FirstOrDefault(p => p.Id == baseLayer.TempID.ToString());
            if (arcBaseLayer != null)
            {
                arcBaseLayer.Opacity = baseLayer.Opacity;
            }
        }

        #endregion 底图

        #region 指北针

        private void RotatePanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MapView.SetViewpointRotationAsync(0);
        }

        #endregion 指北针

        #region 搜索

        private bool isSearchPanelOpen = false;

        private int radius = 1000;

        private PoiInfo[] searchResult;

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
                this.Notify(nameof(radius));
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

        /// <summary>
        /// 使用的搜索引擎
        /// </summary>
        public IPoiEngine SelectedPoiEngine { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is PoiInfo poi)
            {
                MapView.ZoomToGeometryAsync(new MapPoint(poi.Longitude, poi.Latitude, SpatialReferences.Wgs84));
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchResult = null;
            MapView.Overlay.ClearSearchedPois();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
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
                    CoordinateSystem target = SelectedPoiEngine.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;
                    point = CoordinateTransformation.Transformate(point, Config.Instance.BasemapCoordinateSystem, target);
                    SearchResult = await SelectedPoiEngine.SearchAsync(Keyword, point, Radius);
                }
                //视图范围搜索
                else
                {
                    var rect = GeometryEngine.Project(MapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry).TargetGeometry, SpatialReferences.Wgs84) as Envelope;
                    CoordinateSystem target = SelectedPoiEngine.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;
                    rect = CoordinateTransformation.Transformate(rect, Config.Instance.BasemapCoordinateSystem, target) as Envelope;
                    SearchResult = await SelectedPoiEngine.SearchAsync(Keyword, rect);
                }

                //将搜索结果从GCJ02转为WGS84

                MapView.Overlay.ShowSearchedPois(SearchResult);
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

        private void SearchPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isSearchPanelOpen)
            {
                DoubleAnimation aniHeight = new DoubleAnimation(0, Parameters.AnimationDuration)
                { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
                DoubleAnimation aniOpacity = new DoubleAnimation(0, Parameters.AnimationDuration)
                { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
                bdSearchPanel.BeginAnimation(HeightProperty, aniHeight);
                bdSearchPanel.BeginAnimation(OpacityProperty, aniOpacity);
            }
            else
            {
                DoubleAnimation aniHeight = new DoubleAnimation(280, Parameters.AnimationDuration)
                { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
                DoubleAnimation aniOpacity = new DoubleAnimation(1, Parameters.AnimationDuration)
                { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
                bdSearchPanel.BeginAnimation(HeightProperty, aniHeight);
                bdSearchPanel.BeginAnimation(OpacityProperty, aniOpacity);
            }
            isSearchPanelOpen = !isSearchPanelOpen;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        #endregion 搜索
    }
}