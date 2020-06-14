using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using FzLib.UI.Dialog;
using FzLib.DataAnalysis;
using FzLib.Extension;
using GIS.Geometry;
using MapBoard.Common;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static MapBoard.GpxToolbox.SymbolResources;
using MessageBox = FzLib.UI.Dialog.MessageBox;
using Envelope = Esri.ArcGISRuntime.Geometry.Envelope;
using FzLib.UI.Extension;
using Esri.ArcGISRuntime.Symbology;
using System.Diagnostics;
using MapBoard.Common.Dialog;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Drawing;
using static FzLib.Geography.Analysis.SpeedAnalysis;
using FzLib.Geography.IO.Gpx;
using FzLib.Program;
using System.Windows.Threading;
using ProjNet.CoordinateSystems;
using FzLib.Geography.Analysis;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;

namespace MapBoard.GpxToolbox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class BrowseWindow : MainWindowBase
    {

        public BrowseWindow(TrackInfo track)
        {
            BrowseInfo = Config.Instance.BrowseInfo;
            Track = track.Clone();
            Track.Overlay = new GraphicsOverlay() { Renderer = CurrentRenderer };
            InitializeComponent();

            arcMap.InteractionOptions = new SceneViewInteractionOptions() { IsEnabled = false };

        }
        public BrowseInfo BrowseInfo { get; set; } = new BrowseInfo();
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Config.Instance.Save();
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            arcMap.LoadTrack(Track, false, true, false);

        }
        private double progress = 0;
        public double Progress
        {
            get => progress;
            set
            {
                progress = value;
                this.Notify(nameof(Progress));
            }
        }
        private void SpeedChartMouseLeave(object sender, MouseEventArgs e)
        {
            arcMap.ClearSelection();
        }
        bool stopping = false;
        bool playing = false;
        public async Task Play()
        {
            playing = true;
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1.0 / BrowseInfo.FPS),
            };
            var points = track.Track.Points;

            var curve = Calculate.CalculateGeodeticCurve(Ellipsoid.WGS84, points[0], points[1]);
            var cameraPoint = Calculate.CalculateEndingGlobalCoordinates(Ellipsoid.WGS84, points[0], curve.ReverseAzimuth, Math.Sqrt(3) * BrowseInfo.Zoom);
            var camera = new Camera(cameraPoint.Y, cameraPoint.X, BrowseInfo.Zoom, curve.Azimuth.Degrees, 60, 0);
            arcMap.SetLocation(points[0]);
            await arcMap.SetViewpointCameraAsync(camera, TimeSpan.FromSeconds(2));

            int i = 0;
            DateTime startTime = DateTime.Now;
            DateTime startGpxTime = points[0].Time;
            int viewIndex = 0;
            timer.Tick += async (p1, p2) =>
             {
                 DateTime now = DateTime.Now;
                 while (i < points.Count - 2 && (points[i + 1].Time - startGpxTime).Ticks < BrowseInfo.Speed * (now - startTime).Ticks)
                 {
                     i++;
                 }
                 if (i >= points.Count - 2 || stopping)
                 {
                     Stop();
                     timer.Stop();
                     return;
                 }
                 NetTopologySuite.Geometries.Point interPoint = null;
                 Camera camera = null;
                 await Task.Run(() =>
                 {
                     GpxPoint point = points[i];
                     GpxPoint nextPoint = points[i + 1];
                     Progress = 1.0 * (point.Time - points[0].Time).Ticks / (points[points.Count - 1].Time - points[0].Time).Ticks;
                     var curve = Calculate.CalculateGeodeticCurve(Ellipsoid.WGS84, point, nextPoint);
                     double percent =
                     (BrowseInfo.Speed * (now - startTime).Ticks - (point.Time - startGpxTime).Ticks)
                     / (nextPoint.Time - point.Time).Ticks;
                     interPoint = Calculate.CalculateEndingGlobalCoordinates(Ellipsoid.WGS84, point, curve.Azimuth, curve.Length * percent);
                     var cameraPoint = Calculate.CalculateEndingGlobalCoordinates(Ellipsoid.WGS84, point, curve.ReverseAzimuth, Math.Sqrt(3) * BrowseInfo.Zoom);
                     camera = new Camera(cameraPoint.Y, cameraPoint.X, BrowseInfo.Zoom, curve.Azimuth.Degrees, 60, 0);
                 });
                 arcMap.SetLocation(interPoint);
                 if (viewIndex++ % BrowseInfo.Sensitivity == 0)
                 {
                     await arcMap.SetViewpointCameraAsync(camera, TimeSpan.FromSeconds(2.0 / BrowseInfo.FPS * BrowseInfo.Sensitivity));
                 }
             };
            timer.Start();
        }

        private void Stop()
        {
            stopping = false;
            playing = false;
            sldSpeed.IsEnabled = true;

        }

        private void GpxLoaded(object sender, ArcMapView.GpxLoadedEventArgs e)
        {
            ZoomToTrackButtonClick(null, null);
        }


        private TrackInfo track;

        public TrackInfo Track
        {
            get => track;
            set
            {
                SetValueAndNotify(ref track, value, nameof(Track));
            }
        }


        #region 左下角按钮

        private void IdentifyButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.MapTapMode = ArcMapView.MapTapModes.SelectedLayer;
            Cursor = Cursor == Cursors.Help ? Cursors.Arrow : Cursors.Help;
        }
        private void IdentifyAllButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.MapTapMode = ArcMapView.MapTapModes.AllLayers;
            Cursor = Cursor == Cursors.Help ? Cursors.Arrow : Cursors.Help;
        }

        #endregion

        private async void RecoverCameraButtonClick(object sender, RoutedEventArgs e)
        {
            Camera camera = new Camera(arcMap.Camera.Location, 0, 0, 0);
            await arcMap.SetViewpointCameraAsync(camera);


        }

        private void ZoomToTrackButtonClick(object sender, RoutedEventArgs e)
        {
            NetTopologySuite.Geometries.Envelope extent = Track.Track.Points.Extent;
            var esriExtent = new Envelope(extent.MinX, extent.MinY, extent.MaxX, extent.MaxY, SpatialReferences.Wgs84);
            arcMap.SetViewpointAsync(new Viewpoint(esriExtent), TimeSpan.FromSeconds(1));
        }

        private void ArcMapTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (playing)
            {
                stopping = true;
                btnPlay.Content = "播放";
            }
            else
            {
                Play();
                btnPlay.Content = "停止";
                sldSpeed.IsEnabled = false;
            }
        }
    }

}
