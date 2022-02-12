using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using MapBoard.IO.Gpx;
using FzLib.WPF.Dialog;
using ModernWpf.FzExtension.CommonDialog;
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
using static MapBoard.UI.GpxToolbox.SymbolResources;
using FzLib;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using MapBoard.Mapping.Model;
using System.Drawing.Imaging;

namespace MapBoard.UI.GpxToolbox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GpxBrowseWindow : WindowBase
    {
        public GpxBrowseWindow(TrackInfo track)
        {
            BrowseInfo = Config.Instance.Tile_BrowseInfo;
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
            var curve = GeometryUtility.CalculateGeodeticCurve(points[0].ToMapPoint(), points[1].ToMapPoint());
            var cameraPoint = GeometryUtility.CalculateEndingGlobalCoordinates(points[0].ToMapPoint(), curve.ReverseAzimuth, Math.Tan(BrowseInfo.Angle * Math.PI / 180) * BrowseInfo.Zoom);
            var camera = new Camera(cameraPoint.Y, cameraPoint.X, BrowseInfo.Zoom, curve.Azimuth.Degrees, BrowseInfo.Angle, 0);
            arcMap.SetLocation(points[0].ToMapPoint());
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
                 MapPoint interPoint = null;
                 Camera camera = null;
                 await Task.Run(() =>
                 {
                     GpxPoint point = points[i];
                     GpxPoint nextPoint = points[i + 1];
                     Progress = 1.0 * (point.Time - points[0].Time).Ticks / (points[points.Count - 1].Time - points[0].Time).Ticks;
                     var curve = GeometryUtility.CalculateGeodeticCurve(point.ToMapPoint(), nextPoint.ToMapPoint());
                     double percent =
                     (BrowseInfo.Speed * (now - startTime).Ticks - (point.Time - startGpxTime).Ticks)
                     / (nextPoint.Time - point.Time).Ticks;
                     interPoint = GeometryUtility.CalculateEndingGlobalCoordinates(point.ToMapPoint(), curve.Azimuth, curve.Distance * percent);
                     var cameraPoint = GeometryUtility.CalculateEndingGlobalCoordinates(point.ToMapPoint(), curve.ReverseAzimuth, Math.Tan(BrowseInfo.Angle * Math.PI / 180) * BrowseInfo.Zoom);
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
                MapPoint interPoint = null;
                Camera camera = null;
                //计算点位置
                await Task.Run(() =>
                {
                    GpxPoint point = points[i];
                    GpxPoint nextPoint = points[i + 1];
                    Progress = 1.0 * (point.Time - points[0].Time).Ticks / (points[points.Count - 1].Time - points[0].Time).Ticks;
                    var curve = GeometryUtility.CalculateGeodeticCurve(point.ToMapPoint(), nextPoint.ToMapPoint());
                    double percent = 1.0 * (time - point.Time).Ticks / (nextPoint.Time - point.Time).Ticks;
                    interPoint = GeometryUtility.CalculateEndingGlobalCoordinates(point.ToMapPoint(), curve.Azimuth, curve.Distance * percent);
                    var cameraPoint = GeometryUtility.CalculateEndingGlobalCoordinates(point.ToMapPoint(), curve.ReverseAzimuth, Math.Tan(BrowseInfo.Angle * Math.PI / 180) * BrowseInfo.Zoom);
                    camera = new Camera(cameraPoint.Y, cameraPoint.X, BrowseInfo.Zoom, curve.Azimuth.Degrees, BrowseInfo.Angle, 0);
                });
                //设置点的位置
                arcMap.SetLocation(interPoint);
                //设置摄像机位置
                await arcMap.SetViewpointCameraAsync(camera, TimeSpan.Zero);
                //等待渲染完成
                await arcMap.WaitForRenderCompletedAsync();
                //即使已经渲染完成，有时仍然不会立刻显示，需要再等待一段时间
                await Task.Delay(BrowseInfo.ExtraRecordDelay);
                //导出图像
                string filePath = null;
                if (useTimeFileName)
                {
                    filePath = Path.Combine(Parameters.RecordsPath, Track.FileName, time.ToString("yyyyMMdd-HHmmss-fff") + ".png");
                }
                else
                {
                    filePath = Path.Combine(Parameters.RecordsPath, Track.FileName, ++count + ".png");
                }
                await arcMap.ExportImageAsync(filePath, ImageFormat.Png, GeoViewHelper.GetWatermarkThickness());
            }
        }

        private string GetRecordPath()
        {
            return Path.Combine(Parameters.RecordsPath, Track.FileName);
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
                IOUtility.OpenFolder(GetRecordPath());
            }
        }

        private void GpxLoaded(object sender, GpxMapView.GpxLoadedEventArgs e)
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
                this.SetValueAndNotify(ref track, value, nameof(Track));
            }
        }

        #region 左下角按钮

        private void IdentifyButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.MapTapMode = GpxMapView.MapTapModes.SelectedLayer;
            Cursor = Cursor == Cursors.Help ? Cursors.Arrow : Cursors.Help;
        }

        private void IdentifyAllButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.MapTapMode = GpxMapView.MapTapModes.AllLayers;
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
            arcMap.SetViewpointAsync(new Viewpoint(Track.Track.Points.Extent), TimeSpan.FromSeconds(1));
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

        private void RecordButton_Click(object sender, RoutedEventArgs e)
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