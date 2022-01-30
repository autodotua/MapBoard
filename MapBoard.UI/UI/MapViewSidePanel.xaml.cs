using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib;
using MapBoard.Extension;
using MapBoard.Model;
using MapBoard.UI.Dialog;
using MapBoard.Mapping;
using MapBoard.Util;
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
using Esri.ArcGISRuntime.UI.Controls;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FzLib.WPF;
using Esri.ArcGISRuntime.UI;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using Esri.ArcGISRuntime.Location;
using Windows.Devices.Enumeration;
using System.ComponentModel;

namespace MapBoard.UI
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
            BaseLayers = Config.Instance.BaseLayers.Where(p => p.Enable).ToList();
            for (int i = 0; i < BaseLayers.Count; i++)
            {
                BaseLayers[i].Index = i + 1;
            }
            InitializeComponent();
            Config.Instance.PropertyChanged += Config_PropertyChanged;

            //用于限制最多100毫秒更新一次
            timer = new Timer(new TimerCallback(p =>
            {
                lock (lockObj)
                {
                    if (updateScaleAndPositionAction != null)
                    {
                        Dispatcher.InvokeAsync(updateScaleAndPositionAction);
                        updateScaleAndPositionAction = null;
                    }
                    if (setScaleAction != null)
                    {
                        Dispatcher.InvokeAsync(setScaleAction);
                        setScaleAction = null;
                    }
                }
            }), null, 100, 100);
        }

        public GeoView MapView { get; private set; }

        public void Initialize(GeoView mapView)
        {
            MapView = mapView;
            mapView.ViewpointChanged += MapView_ViewpointChanged;
            mapView.PreviewMouseMove += MapView_PreviewMouseMove;
            searchPanel.Initialize(mapView);
            routePanel.Initialize(mapView);
            reGeoCodePanel.Initialize(mapView);
            if (mapView is SceneView)
            {
                bdViewPointInfo.Visibility = Visibility.Collapsed;
                bdScale.Visibility = Visibility.Collapsed;
                bdLocation.Visibility = Visibility.Collapsed;
                bdZoomIn.Visibility = Visibility.Collapsed;
                bdZoomOut.Visibility = Visibility.Collapsed;
            }
            else if (mapView is MapView m)
            {
                m.LocationDisplay.AutoPanModeChanged += LocationDisplay_AutoPanModeChanged;
                m.LocationDisplay.LocationChanged += (s, e) => UpdateLocation();
                m.LocationDisplay.DataSource.StatusChanged += (s, e) => UpdateLocation();
                m.LocationDisplay.DataSource.HeadingChanged += (s, e) => UpdateLocation();
            }
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private string scaleBarLength;

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

        public string ScaleBarLength
        {
            get => scaleBarLength;
            private set => this.SetValueAndNotify(ref scaleBarLength, value, nameof(ScaleBarLength));
        }

        private string scale;

        public string Scale
        {
            get => scale;
            set => this.SetValueAndNotify(ref scale, value, nameof(Scale));
        }

        private bool dataSourceOn = true;

        public bool DataSourceOn
        {
            get => dataSourceOn;
            set
            {
                Debug.Assert(MapView is MapView);
                if (value != dataSourceOn)
                {
                    if (value)
                    {
                        (MapView as MapView).LocationDisplay.DataSource.StartAsync();
                    }
                    else
                    {
                        (MapView as MapView).LocationDisplay.DataSource.StopAsync();
                    }
                    this.SetValueAndNotify(ref dataSourceOn, value, nameof(DataSourceOn));
                }
            }
        }

        public void UpdateScaleAndPosition(Point? position = null)
        {
            if (!IsLoaded
                || MapView == null
                || MapView is not Esri.ArcGISRuntime.UI.Controls.MapView
                || (MapView as MapView).Map == null)
            {
                return;
            }
            var view = MapView as MapView;
            updateScaleAndPositionAction = () =>
            {
                if (position.HasValue)
                {
                    location = view.ScreenToLocation(position.Value);
                }
                if (location != null)
                {
                    location = GeometryEngine.Project(location, SpatialReferences.Wgs84) as MapPoint;
                    Latitude = location.Y.ToString("0.000000");
                    Longitude = location.X.ToString("0.000000");
                    ScaleBarLength = (view.UnitsPerPixel * ActualWidth * Math.Cos(Math.PI / 180 * location.Y)).ToString("0.00m");
                }
                Scale = view.MapScale > 10000 ? (view.MapScale / 10000).ToString("0.00万") : view.MapScale.ToString("0");
                var level = 5 * (20 + Math.Log(view.Map.MaxScale / view.MapScale, 2));
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
            if (MapView is SceneView s)
            {
                (vbxRotate.RenderTransform as RotateTransform).Angle = -s.Camera.Heading;
            }
            else if (MapView is MapView m)
            {
                (vbxRotate.RenderTransform as RotateTransform).Angle = -m.MapRotation;
                if (IsLoaded && MapView.IsLoaded)
                {
                    UpdateScaleAndPosition();
                }
            }
        }

        #endregion 右下角坐标和比例尺

        #region 缩放按钮和缩放条

        private double mapScalePercent = 0;
        private string scaleLevel = "50%";

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
                double target = Config.Instance.MaxScale / Math.Pow(2, value / 5 - 20);
                if (MapView is MapView m)
                {
                    setScaleAction = () => m.SetViewpointScaleAsync(target);
                }
                else if (MapView is SceneView s)
                {
                    throw new NotSupportedException();
                    //setScaleAction = () => m.SetViewpointScaleAsync(MapView.Map.MaxScale / Math.Pow(2, value / 5 - 20));
                }
            }
        }

        public string ScaleLevel
        {
            get => scaleLevel;
            private set => this.SetValueAndNotify(ref scaleLevel, value, nameof(ScaleLevel));
        }

        private void PanelScale_MouseLeave(object sender, MouseEventArgs e)
        {
            OpenOrCloseScalePanelAsync(false);
        }

        private async Task OpenOrCloseScalePanelAsync(bool open)
        {
            Storyboard storyboard = new Storyboard();
            new DoubleAnimation(open ? 240 : 36, Parameters.AnimationDuration)
                .SetInOutCubicEase()
                .SetStoryboard(HeightProperty, bdScale)
                .AddTo(storyboard);
            new DoubleAnimation(open ? 0 : 1, Parameters.AnimationDuration)
                .SetInOutCubicEase()
                .SetStoryboard(OpacityProperty, tbkScale)
                .AddTo(storyboard);
            new DoubleAnimation(open ? 1 : 0, Parameters.AnimationDuration)
                .SetInOutCubicEase()
                .SetStoryboard(OpacityProperty, grdScale)
                .AddTo(storyboard);
            await storyboard.BeginAsync();
        }

        private void ScaleButton_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenOrCloseScalePanelAsync(true);
        }

        private async void ZoomInOutButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (MapView is MapView m)
            {
                double r = ((sender as FrameworkElement).Tag as string).Equals("1") ? 1.0 / 3 : 3;
                await m.SetViewpointScaleAsync(m.MapScale * r);
            }
            else if (MapView is SceneView s)
            {
                throw new NotSupportedException();
            }
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
                OpenOrCloseLayersPanelAsync(false);
            }
        }

        private void LayersPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isLayerPanelOpened)
            {
                OpenOrCloseLayersPanelAsync(true);
            }
        }

        /// <summary>
        /// 单击底图的可见单选框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var baseLayer = (sender as FrameworkElement).Tag as BaseLayerInfo;
            Basemap basemap = null;
            if (MapView is MapView m)
            {
                basemap = m.Map.Basemap;
            }
            else if (MapView is SceneView s)
            {
                basemap = s.Scene.Basemap;
            }
            var arcBaseLayer = basemap.BaseLayers.FirstOrDefault(p => p.Id == baseLayer.TempID.ToString());
            if (arcBaseLayer != null)
            {
                arcBaseLayer.IsVisible = baseLayer.Visible;
            }
        }

        private void CloseLayerPanelButton_Click(object sender, RoutedEventArgs e)
        {
            OpenOrCloseLayersPanelAsync(false);
        }

        private async Task OpenOrCloseLayersPanelAsync(bool open)
        {
            isLayerPanelOpened = open;

            Storyboard storyboard = new Storyboard();
            new DoubleAnimation(open ? 360 : 36, Parameters.AnimationDuration)
                   .SetInOutCubicEase()
                   .SetStoryboard(WidthProperty, bdLayers)
                   .AddTo(storyboard);
            new DoubleAnimation(open ? 360 : 36, Parameters.AnimationDuration)
                 .SetInOutCubicEase()
                 .SetStoryboard(HeightProperty, bdLayers)
                 .AddTo(storyboard);
            new DoubleAnimation(open ? 0 : 1, Parameters.AnimationDuration)
                .SetInOutCubicEase()
                .SetStoryboard(OpacityProperty, iconLayers)
                .AddTo(storyboard);
            new DoubleAnimation(open ? 1 : 0, Parameters.AnimationDuration)
                    .SetInOutCubicEase()
                    .SetStoryboard(OpacityProperty, grdLayers)
                    .AddTo(storyboard);

            bdLayers.IsHitTestVisible = false;
            if (open)
            {
                grdLayers.Visibility = Visibility.Visible;
                bdLayers.Cursor = Cursors.Arrow;
                dgLayers.Focus();
                await storyboard.BeginAsync();
            }
            else
            {
                bdLayers.Cursor = Cursors.Hand;
                await storyboard.BeginAsync();
                grdLayers.Visibility = Visibility.Collapsed;
            }
            bdLayers.IsHitTestVisible = true;
        }

        private void OpenSettingDialogButton_Click(object sender, RoutedEventArgs e)
        {
            OpenOrCloseLayersPanelAsync(false);
            new SettingDialog(Window.GetWindow(this), (MapView as IMapBoardGeoView).Layers, 3).ShowDialog();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var baseLayer = (sender as FrameworkElement).Tag as BaseLayerInfo;
            Basemap basemap = null;
            if (MapView is MapView m)
            {
                basemap = m.Map.Basemap;
            }
            else if (MapView is SceneView s)
            {
                basemap = s.Scene.Basemap;
            }
            var arcBaseLayer = basemap.BaseLayers.FirstOrDefault(p => p.Id == baseLayer.TempID.ToString());
            if (arcBaseLayer != null)
            {
                arcBaseLayer.Opacity = baseLayer.Opacity;
            }
        }

        #endregion 底图

        #region 指北针

        private async void RotatePanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MapView is MapView m)
            {
                m.SetViewpointRotationAsync(0);
            }
            else if (MapView is SceneView s)
            {
                Camera camera = new Camera(s.Camera.Location, 0, 0, 0);
                await s.SetViewpointCameraAsync(camera);
            }
        }

        #endregion 指北针

        #region 搜索

        private bool isSearchPanelOpened = false;

        private void SearchPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenOrCloseSearchPanelAsync(true);
        }

        private void CloseSearchPanelButton_Click(object sender, RoutedEventArgs e)
        {
            OpenOrCloseSearchPanelAsync(false);
        }

        private async Task OpenOrCloseSearchPanelAsync(bool open)
        {
            if (isSearchPanelOpened && open || !isSearchPanelOpened && !open)
            {
                return;
            }
            bdSearch.IsHitTestVisible = false;
            isSearchPanelOpened = open;
            vwSearchIcon.IsHitTestVisible = !open;
            grdSearchPanel.IsHitTestVisible = open;

            Storyboard storyboard = new Storyboard();
            new DoubleAnimation(open ? 400 : 36, Parameters.AnimationDuration)
                .SetInOutCubicEase()
                .SetStoryboard(HeightProperty, bdSearch)
                .AddTo(storyboard);

            new DoubleAnimation(open ? 360 : 36, Parameters.AnimationDuration)
                           .SetInOutCubicEase()
                            .SetStoryboard(WidthProperty, bdSearch)
                            .AddTo(storyboard);

            new DoubleAnimation(open ? 1 : 0, Parameters.AnimationDuration)
               .SetInOutCubicEase()
                .SetStoryboard(OpacityProperty, grdSearchPanel)
                .AddTo(storyboard);
            new DoubleAnimation(open ? 0 : 1, Parameters.AnimationDuration)
                .SetInOutCubicEase()
                .SetStoryboard(OpacityProperty, vwSearchIcon)
                .AddTo(storyboard);
            await storyboard.BeginAsync();
            bdSearch.IsHitTestVisible = true;
            bdSearch.Cursor = open ? Cursors.Arrow : Cursors.Hand;
        }

        #endregion 搜索

        #region 定位

        private bool isLocationPanelOpened = false;
        private int panMode = 0;
        private string locationStatus;

        public string LocationStatus
        {
            get => locationStatus;
            set => this.SetValueAndNotify(ref locationStatus, value, nameof(LocationStatus));
        }

        public int PanMode
        {
            get => panMode;
            set
            {
                if (panMode != value)
                {
                    this.SetValueAndNotify(ref panMode, value, nameof(PanMode));

                    (MapView as MapView).LocationDisplay.AutoPanMode = (LocationDisplayAutoPanMode)value;
                }
            }
        }

        public ObservableCollection<PropertyNameValue> LocationProperties { get; } = new ObservableCollection<PropertyNameValue>();

        private async Task OpenOrCloseLocationPanelAsync(bool open)
        {
            isLocationPanelOpened = open;

            Storyboard storyboard = new Storyboard();
            new DoubleAnimation(open ? 360 : 36, Parameters.AnimationDuration)
                   .SetInOutCubicEase()
                   .SetStoryboard(WidthProperty, bdLocation)
                   .AddTo(storyboard);
            new DoubleAnimation(open ? 260 : 36, Parameters.AnimationDuration)
                 .SetInOutCubicEase()
                 .SetStoryboard(HeightProperty, bdLocation)
                 .AddTo(storyboard);

            new DoubleAnimation(open ? 0 : 1, Parameters.AnimationDuration)
          .SetInOutCubicEase()
          .SetStoryboard(OpacityProperty, iconLocation)
          .AddTo(storyboard);
            new DoubleAnimation(open ? 1 : 0, Parameters.AnimationDuration)
                    .SetInOutCubicEase()
                    .SetStoryboard(OpacityProperty, grdLocation)
                    .AddTo(storyboard);

            bdLocation.IsHitTestVisible = false;

            if (open)
            {
                InitializeLocationInfos();
                grdLocation.Visibility = Visibility.Visible;
                bdLocation.Cursor = Cursors.Arrow;
                await storyboard.BeginAsync();
            }
            else
            {
                bdLocation.Cursor = Cursors.Hand;
                await storyboard.BeginAsync();
                grdLocation.Visibility = Visibility.Collapsed;
            }
            bdLocation.IsHitTestVisible = true;
        }

        private void InitializeLocationInfos()
        {
            Debug.Assert(MapView is MapView);

            var ld = (MapView as MapView).LocationDisplay;
            UpdateLocation();
            PanMode = (int)ld.AutoPanMode;
        }

        private bool isUpdatingLocation = false;

        private void UpdateLocation()
        {
            if (!isLocationPanelOpened || isUpdatingLocation)
            {
                return;
            }
            Dispatcher.Invoke(async () =>
            {
                List<PropertyNameValue> newProperties = new List<PropertyNameValue>();
                isUpdatingLocation = true;
                var ld = (MapView as MapView).LocationDisplay;
                await Task.Run(() =>
                {
                    Esri.ArcGISRuntime.Location.Location l = ld.Location;
                    LocationStatus = ld.DataSource.Status switch
                    {
                        LocationDataSourceStatus.Stopped => "已停止",
                        LocationDataSourceStatus.Starting => "正在定位",
                        LocationDataSourceStatus.Started => "已定位",
                        LocationDataSourceStatus.Stopping => "正在停止",
                        LocationDataSourceStatus.FailedToStart => "定位失败",
                        _ => throw new NotImplementedException()
                    };

                    if (l != null)
                    {
                        newProperties.Add(new PropertyNameValue("经度", l.Position.X.ToString("0.000000°")));
                        newProperties.Add(new PropertyNameValue("纬度", l.Position.Y.ToString("0.000000°")));
                        newProperties.Add(new PropertyNameValue("水平精度", l.HorizontalAccuracy.ToString("0m")));
                        if (l.Position.Z != 0 && !double.IsNaN(l.VerticalAccuracy))
                        {
                            newProperties.Add(new PropertyNameValue("海拔", l.Position.Z.ToString("0m")));
                            newProperties.Add(new PropertyNameValue("垂直精度", l.VerticalAccuracy.ToString("0m")));
                        }
                    }
                    if (!double.IsNaN(ld.Heading))
                    {
                        newProperties.Add(new PropertyNameValue("指向", ld.Heading.ToString("0.0°")));
                    }
                    if (l != null)
                    {
                        if (l.Timestamp.HasValue)
                        {
                            newProperties.Add(new PropertyNameValue("定位时间", l.Timestamp.Value.LocalDateTime.ToString()));
                        }
                        newProperties.Add(new PropertyNameValue("过时定位", l.IsLastKnown ? "是" : "否"));

                        if (l.AdditionalSourceProperties.ContainsKey("positionSource"))
                        {
                            newProperties.Add(new PropertyNameValue("来源", l.AdditionalSourceProperties["positionSource"].ToString()));
                        }
                        foreach (var key in l.AdditionalSourceProperties.Keys.Where(p => p != "positionSource"))
                        {
                            object value = l.AdditionalSourceProperties[key];
                            if (value is double d)
                            {
                                value = d.ToString("0.00");
                            }
                            newProperties.Add(new PropertyNameValue(key, value.ToString()));
                        }
                    }
                });
                foreach (var property in newProperties)
                {
                    var oldProperty = LocationProperties.FirstOrDefault(p => p.Name == property.Name);
                    if (oldProperty != null)
                    {
                        oldProperty.Value = property.Value;
                    }
                    else
                    {
                        LocationProperties.Add(property);
                    }
                }
                foreach (var property in LocationProperties.ToList())
                {
                    if (!newProperties.Any(p => p.Name == property.Name))
                    {
                        LocationProperties.Remove(property);
                    }
                }
                isUpdatingLocation = false;
            });
        }

        private void iconLocation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isLocationPanelOpened)
            {
                OpenOrCloseLocationPanelAsync(true);
            }
        }

        private void CloseLocationPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLocationPanelOpened)
            {
                OpenOrCloseLocationPanelAsync(false);
            }
        }

        private void LocationDisplay_AutoPanModeChanged(object sender, LocationDisplayAutoPanMode e)
        {
            PanMode = (int)e;
        }

        #endregion 定位
    }

    public class PropertyNameValue : INotifyPropertyChanged
    {
        public PropertyNameValue()
        {
        }

        public PropertyNameValue(string name, string value)
        {
            Name = name;
            Value = value;
        }

        private string name;

        public string Name
        {
            get => name;
            set => this.SetValueAndNotify(ref name, value, nameof(Name));
        }

        private string v;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Value
        {
            get => v;
            set => this.SetValueAndNotify(ref v, value, nameof(Value));
        }
    }
}