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
            BaseLayers = Config.Instance.BaseLayers;
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
            }
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
                    Scale = (view.UnitsPerPixel * ActualWidth * Math.Cos(Math.PI / 180 * location.Y)).ToString("0.00m");
                }
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
            new SettingDialog(Window.GetWindow(this), (MapView as IMapBoardGeoView).Layers).ShowDialog();
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
            if (isSearchPanelOpened)
            {
                return;
            }
            isSearchPanelOpened = true;
            vwSearchIcon.IsHitTestVisible = false;
            grdSearchPanel.IsHitTestVisible = true;
            DoubleAnimation aniHeight = new DoubleAnimation(400, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniWidth = new DoubleAnimation(360, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniOpacity = new DoubleAnimation(1, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniIconOpacity = new DoubleAnimation(0, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            bdSearch.BeginAnimation(HeightProperty, aniHeight);
            bdSearch.BeginAnimation(WidthProperty, aniWidth);
            grdSearchPanel.BeginAnimation(OpacityProperty, aniOpacity);
            vwSearchIcon.BeginAnimation(OpacityProperty, aniIconOpacity);
            bdSearch.Cursor = Cursors.Arrow;
        }

        #endregion 搜索

        private void CloseSearchPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isSearchPanelOpened)
            {
                return;
            }
            isSearchPanelOpened = false;
            vwSearchIcon.IsHitTestVisible = true;
            grdSearchPanel.IsHitTestVisible = false;
            DoubleAnimation aniHeight = new DoubleAnimation(36, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniWidth = new DoubleAnimation(36, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniOpacity = new DoubleAnimation(0, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            DoubleAnimation aniIconOpacity = new DoubleAnimation(1, Parameters.AnimationDuration)
            { EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut } };
            bdSearch.BeginAnimation(HeightProperty, aniHeight);
            bdSearch.BeginAnimation(WidthProperty, aniWidth);
            grdSearchPanel.BeginAnimation(OpacityProperty, aniOpacity);
            vwSearchIcon.BeginAnimation(OpacityProperty, aniIconOpacity);
            bdSearch.Cursor = Cursors.Hand;
        }
    }
}