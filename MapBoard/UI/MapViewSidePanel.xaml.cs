using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Common.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapBoard.Main.UI
{
    /// <summary>
    /// MapViewInfoPanel.xaml 的交互逻辑
    /// </summary>
    public partial class MapViewSidePanel : UserControlBase
    {
        private Timer timer;
        private Action updateScaleAndPositionAction;
        private Action setScaleAction;
        private object lockObj = new object();

        public MapViewSidePanel()
        {
            InitializeComponent();
            Config.Instance.PropertyChanged += Instance_PropertyChanged;
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

        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.BaseLayers))
            {
                BaseLayers = Config.Instance.BaseLayers;
            }
        }

        private MapPoint location;
        public ArcMapView MapView { get; private set; }

        public void Initialize(ArcMapView mapView)
        {
            MapView = mapView;
            mapView.ViewpointChanged += MapView_ViewpointChanged;
            mapView.PreviewMouseMove += MapView_PreviewMouseMove;
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

        private string latitude;

        public string Latitude
        {
            get => latitude;
            private set => this.SetValueAndNotify(ref latitude, value, nameof(Latitude));
        }

        private string longitude;

        public string Longitude
        {
            get => longitude;
            private set => this.SetValueAndNotify(ref longitude, value, nameof(Longitude));
        }

        private string scale;

        public string Scale
        {
            get => scale;
            private set => this.SetValueAndNotify(ref scale, value, nameof(Scale));
        }

        private string scaleLevel;

        public string ScaleLevel
        {
            get => scaleLevel;
            private set => this.SetValueAndNotify(ref scaleLevel, value, nameof(ScaleLevel));
        }

        private double mapScalePercent = 0;

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

        private List<BaseLayerInfo> baseLayers;

        public List<BaseLayerInfo> BaseLayers
        {
            get => baseLayers;
            set => this.SetValueAndNotify(ref baseLayers, value, nameof(BaseLayers));
        }

        private bool isLayerPanelOpened = false;

        private void bdLayers_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isLayerPanelOpened)
            {
                OpenLayersPanel();
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

        private void bdLayers_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isLayerPanelOpened && !IsMouseOver)
            {
                CloseLayersPanel();
            }
        }

        private void dgLayers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Column.DisplayIndex == 1)
                {
                }
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

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var baseLayer = (sender as FrameworkElement).Tag as BaseLayerInfo;
            var arcBaseLayer = MapView.Map.Basemap.BaseLayers.FirstOrDefault(p => p.Id == baseLayer.TempID.ToString());
            if (arcBaseLayer != null)
            {
                arcBaseLayer.Opacity = baseLayer.Opacity;
            }
        }

        private void vbxRotate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MapView.SetViewpointRotationAsync(0);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CloseLayersPanel();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CloseLayersPanel();
            new SettingDialog(MapView) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void bdScale_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenScalePanel();
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

        private void bdScale_MouseLeave(object sender, MouseEventArgs e)
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
    }
}