using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using FzLib.Geography.Analysis;
using FzLib.Geography.IO.Gpx;
using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.GpxToolbox.Model;
using ModernWpf.FzExtension.CommonDialog;
using ProjNet.CoordinateSystems;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Envelope = Esri.ArcGISRuntime.Geometry.Envelope;
using static MapBoard.GpxToolbox.Model.SymbolResources;

namespace MapBoard.GpxToolbox.UI
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

        private void WindowLoaded(object sender, RoutedEventArgs e)
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

        private bool stopping = false;
        private bool working = false;

        private void SetToFirstPoint()
        {
            var points = track.Track.Points;
            var curve = Calculate.CalculateGeodeticCurve(Ellipsoid.WGS84, points[0], points[1]);
            var cameraPoint = Calculate.CalculateEndingGlobalCoordinates(Ellipsoid.WGS84, points[0], curve.ReverseAzimuth, Math.Tan(BrowseInfo.Angle * Math.PI / 180) * BrowseInfo.Zoom);
            var camera = new Camera(cameraPoint.Y, cameraPoint.X, BrowseInfo.Zoom, curve.Azimuth.Degrees, BrowseInfo.Angle, 0);
            arcMap.SetLocation(points[0]);
            arcMap.SetViewpointCameraAsync(camera, TimeSpan.Zero);
        }

        public void StartPlay()
        {
            working = true;
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1.0 / BrowseInfo.FPS),
            };
            var points = track.Track.Points;

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
                     StopPlay();
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
                     var cameraPoint = Calculate.CalculateEndingGlobalCoordinates(Ellipsoid.WGS84, point, curve.ReverseAzimuth, Math.Tan(BrowseInfo.Angle * Math.PI / 180) * BrowseInfo.Zoom);
                     camera = new Camera(cameraPoint.Y, cameraPoint.X, BrowseInfo.Zoom, curve.Azimuth.Degrees, BrowseInfo.Angle, 0);
                 });
                 arcMap.SetLocation(interPoint);
                 if (viewIndex++ % BrowseInfo.Sensitivity == 0)
                 {
                     await arcMap.SetViewpointCameraAsync(camera, TimeSpan.FromSeconds(2.0 / BrowseInfo.FPS * BrowseInfo.Sensitivity));
                 }
             };
            timer.Start();
        }

        public async Task Record()
        {
            working = true;
            if (!Directory.Exists(GetRecordPath()))
            {
                Directory.CreateDirectory(GetRecordPath());
            }
            var points = track.Track.Points;
            DateTime startTime = points.First().Time;
            int i = 0;
            int count = 0;
            bool useTimeFileName = rbtnFormatTime.IsChecked.Value;
            for (DateTime time = startTime; ; time = time.AddMilliseconds(BrowseInfo.RecordInterval))
            {
                while (i < points.Count - 2 && points[i + 1].Time < time)
                {
                    i++;
                }
                if (i >= points.Count - 2 || stopping)
                {
                    await StopRecordAsync();
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
                    double percent = 1.0 * (time - point.Time).Ticks / (nextPoint.Time - point.Time).Ticks;
                    interPoint = Calculate.CalculateEndingGlobalCoordinates(Ellipsoid.WGS84, point, curve.Azimuth, curve.Length * percent);
                    var cameraPoint = Calculate.CalculateEndingGlobalCoordinates(Ellipsoid.WGS84, point, curve.ReverseAzimuth, Math.Tan(BrowseInfo.Angle * Math.PI / 180) * BrowseInfo.Zoom);
                    camera = new Camera(cameraPoint.Y, cameraPoint.X, BrowseInfo.Zoom, curve.Azimuth.Degrees, BrowseInfo.Angle, 0);
                });
                arcMap.SetLocation(interPoint);
                await arcMap.SetViewpointCameraAsync(camera, TimeSpan.Zero);
                await Task.Delay(100);
                await Task.Run(() =>
                {
                    var image = arcMap.ExportImageAsync().Result.ToImageSourceAsync().Result as BitmapSource;
                    string filePath = null;
                    if (useTimeFileName)
                    {
                        filePath = Path.Combine(Config.RecordPath, Track.FileName, time.ToString("yyyyMMdd-HHmmss-fff") + ".png");
                    }
                    else
                    {
                        filePath = Path.Combine(Config.RecordPath, Track.FileName, ++count + ".png");
                    }
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fileStream);
                });
            }
        }

        private string GetRecordPath()
        {
            return Path.Combine(Config.RecordPath, Track.FileName);
        }

        private void StopPlay()
        {
            stopping = false;
            working = false;
            SetUIEnabled(false, false);
        }

        private async Task StopRecordAsync()
        {
            btnRecord.Content = "录制";
            stopping = false;
            working = false;
            SetUIEnabled(false, false);
            ResizeMode = ResizeMode.CanResize;
            if (await CommonDialog.ShowYesNoDialogAsync("是否打开目录？") == true)
            {
                Process.Start("explorer.exe", GetRecordPath());
            }
        }

        private void GpxLoaded(object sender, ArcMapView.GpxLoadedEventArgs e)
        {
            //ZoomToTrackButtonClick(null, null);
            SetToFirstPoint();

            BrowseInfo.PropertyChanged += (p1, p2) =>
            {
                if (p2.PropertyName == nameof(BrowseInfo.Zoom)
                || p2.PropertyName == nameof(BrowseInfo.Angle))
                {
                    SetToFirstPoint();
                }
            };
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

        #endregion 左下角按钮

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
            if (working)
            {
                stopping = true;
                btnPlay.Content = "播放";
                SetUIEnabled(false, false);
            }
            else
            {
                StartPlay();
                btnPlay.Content = "停止";
                SetUIEnabled(true, false);
            }
        }

        private void SetUIEnabled(bool working, bool record)
        {
            grdPlay.IsEnabled = !stopping && (!working || !record);
            grdRecord.IsEnabled = !stopping && (!working || record);
            sldSpeed.IsEnabled = !working;
            grdCommon.IsEnabled = !working && !stopping;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (working)
            {
                stopping = true;
                btnRecord.Content = "录制";
                SetUIEnabled(false, true);
            }
            else
            {
                Record();
                btnRecord.Content = "停止";
                SetUIEnabled(true, true);
                ResizeMode = ResizeMode.NoResize;
            }
        }
    }
}