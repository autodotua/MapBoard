using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.GpxToolbox.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private object lockObj = new object();
        private bool canUpdate = true;
        private Timer timer;

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
                     canUpdate = true;
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
        public MapView MapView { get; private set; }

        public void Initialize(MapView mapView)
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
            lock (lockObj)
            {
                if (!canUpdate)
                {
                    return;
                }
                canUpdate = false;
            }
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
            DoubleAnimation aniHeight = new DoubleAnimation(180, Parameters.AnimationDuration)
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
    }
}