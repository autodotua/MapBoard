using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using FzLib.DataAnalysis;
using FzLib;
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
using static MapBoard.UI.GpxToolbox.SymbolResources;
using Envelope = Esri.ArcGISRuntime.Geometry.Envelope;
using Esri.ArcGISRuntime.Symbology;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Drawing;
using MapBoard.IO.Gpx;
using FzLib.Program;
using System.Collections.ObjectModel;
using ModernWpf.FzExtension.CommonDialog;
using System.Threading.Tasks;
using FzLib.WPF.Dialog;
using MapBoard.Util;
using MapBoard.Mapping;
using static MapBoard.IO.Gpx.GpxSpeedAnalysis;
using MapBoard.Mapping.Model;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib.WPF.Controls;
using FzLib.WPF;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MapBoard.UI.GpxToolbox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GpxWindow : MainWindowBase
    {
        public ObservableCollection<TrackInfo> Tracks { get; } = new ObservableCollection<TrackInfo>();

        private string[] loadNeeded = null;
        private TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint> chartHelper;

        public GpxWindow(string[] load = null) : this()
        {
            loadNeeded = load;
        }

        public GpxWindow()
        {
            InitializeComponent();
            arcMap.Tracks = Tracks;
            InitializeChart();
            ListViewHelper<TrackInfo> lvwHelper = new ListViewHelper<TrackInfo>(lvwFiles);
            lvwHelper.EnableDragAndDropItem();
        }

        protected async override Task InitializeAsync()
        {
            if (loadNeeded != null)
            {
                await arcMap.LoadFilesAsync(loadNeeded);
            }
            else if (File.Exists(Parameters.TrackHistoryPath))
            {
                string[] files = File.ReadAllLines(Parameters.TrackHistoryPath);
                await DoAsync(() => arcMap.LoadFilesAsync(files), "正在导入轨迹");
            }
        }

        private void InitializeChart()
        {
            chartHelper = new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>(speedChart);

            chartHelper.XAxisLineValueConverter = p => p.CenterTime;
            chartHelper.XAxisPointValueConverter = p => p.CenterTime;
            chartHelper.XAxisPolygonValueConverter = p => p.Time;

            chartHelper.YAxisPointValueConverter = p => p.Speed;
            chartHelper.YAxisLineValueConverter = p => p.Speed;
            chartHelper.YAxisPolygonValueConverter = p => p.Z;
            chartHelper.XLabelFormat = p => p.ToString("HH:mm");
            chartHelper.YLabelFormat = p => $"{p}m/s {(p * 3.6).ToString(".0")}km/h";
            chartHelper.ToolTipConverter = p =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(p.CenterTime.ToString("HH:mm:ss"));
                sb.Append(p.Speed.ToString("0.00")).AppendLine("m/s");
                sb.Append((3.6 * p.Speed).ToString("0.00")).AppendLine("km/h");
                if (!double.IsNaN(p.RelatedPoints[0].Z) && !double.IsNaN(p.RelatedPoints[1].Z))
                {
                    sb.Append(((p.RelatedPoints[0].Z + p.RelatedPoints[1].Z) / 2).ToString("0.00") + "m");
                }
                return sb.ToString();
            };

            chartHelper.MouseOverPoint += (p1, p2) =>
            {
                //arcMap.ClearSelection();
                arcMap.SelectPointTo(p2.Item.RelatedPoints[0]);
            };
            chartHelper.LinePointEnbale = (p1, p2) => (p2.CenterTime - p1.CenterTime) < TimeSpan.FromSeconds(200);
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            File.WriteAllLines(Parameters.TrackHistoryPath, Tracks.Select(p => p.FilePath).ToArray());
            Tracks.Clear();
            // map.Dispose();
        }

        private void ListViewItemPreviewDeleteKeyDown(object sender, KeyEventArgs e)
        {
            if (e == null || e.Key == Key.Delete)
            {
                foreach (var item in lvwFiles.SelectedItems.Cast<TrackInfo>().ToArray())
                {
                    arcMap.GraphicsOverlays.Remove(item.Overlay);
                    Tracks.Remove(item);
                }
            }
        }

        private async void FileSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arcMap.SelectedTrack != null)
            {
                arcMap.SelectedTrack.Overlay.Renderer = null;
                arcMap.SelectedTrack.Overlay.Renderer = NormalRenderer;
            }
            if (lvwFiles.SelectedItem == null)
            {
                arcMap.SelectedTrack = null;
                Gpx = null;
                GpxTrack = null;
                chartHelper.Initialize();
                return;
            }

            if (lvwFiles.SelectedItems.Count > 1)
            {
                arcMap.SelectedTrack = null;
                Gpx = null;
                GpxTrack = null;

                chartHelper.Initialize();
            }
            else
            {
                arcMap.SelectedTrack = lvwFiles.SelectedItem as TrackInfo;

                arcMap.SelectedTrack.Overlay.Renderer = CurrentRenderer;
                //arcMap.SelectedTrack.Overlay.Graphics[0].Symbol = CurrentLineSymbol;

                Gpx = arcMap.SelectedTrack.Gpx;
                GpxTrack = arcMap.SelectedTrack.Track;
                await Task.WhenAll(ZoomToTrackAsync(), UpdateUI());
            }
        }

        /// <summary>
        /// 刷新轨迹的信息、表格和图表
        /// </summary>
        /// <returns></returns>
        private async Task UpdateUI()
        {
            try
            {
                var pointsTask = GetUsableSpeedsAsync(GpxTrack.Points);
                var linesTask = GetMeanFilteredSpeedsAsync(GpxTrack.Points, 20, 5);
                await Task.WhenAll(pointsTask, linesTask);
                chartHelper.DrawActionAsync = () =>
                    DrawChartAsync(pointsTask.Result, linesTask.Result);

                await Task.Run(() =>
             {
                 var speed = arcMap.SelectedTrack.Track.AverageSpeed;

                 SpeedText = speed.ToString("0.00") + "m/s    " + (speed * 3.6).ToString("0.00") + "km/h";
                 DistanceText = (arcMap.SelectedTrack.Track.Distance / 1000).ToString("0.00") + "km";

                 var movingSpeed = arcMap.SelectedTrack.Track.GetMovingAverageSpeed();
                 MovingSpeedText = movingSpeed.ToString("0.00") + "m/s    " + (movingSpeed * 3.6).ToString("0.00") + "km/h";
                 MovingTimeText = arcMap.SelectedTrack.Track.GetMovingTime().ToString();

                 var maxSpeed = arcMap.SelectedTrack.Track.GetMaxSpeedAsync().Result;
                 MaxSpeedText = maxSpeed.ToString("0.00") + "m/s    " + (maxSpeed * 3.6).ToString("0.00") + "km/h";
             });

                chartHelper.BeginDraw();
            }
            catch (Exception ex)
            {
            }
        }

        private string speedText;

        public string SpeedText
        {
            get => speedText;
            set => this.SetValueAndNotify(ref speedText, value, nameof(SpeedText));
        }

        private string distanceText;

        public string DistanceText
        {
            get => distanceText;
            set => this.SetValueAndNotify(ref distanceText, value, nameof(DistanceText));
        }

        private string movingSpeedText;

        public string MovingSpeedText
        {
            get => movingSpeedText;
            set => this.SetValueAndNotify(ref movingSpeedText, value, nameof(MovingSpeedText));
        }

        private string movingTimeText;

        public string MovingTimeText
        {
            get => movingTimeText;
            set => this.SetValueAndNotify(ref movingTimeText, value, nameof(MovingTimeText));
        }

        private string maxSpeedText;

        public string MaxSpeedText
        {
            get => maxSpeedText;
            set => this.SetValueAndNotify(ref maxSpeedText, value, nameof(MaxSpeedText));
        }

        private async Task DrawChartAsync(IEnumerable<SpeedInfo> points, IReadOnlyList<SpeedInfo> lines)
        {
            try
            {
                chartHelper.Initialize();
                await Task.WhenAll(
                 chartHelper.DrawBorderAsync(lines, true,
                       new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.BorderSetting<SpeedInfo>()
                       {
                           XAxisBorderValueConverter = p => p.CenterTime,
                           YAxisBorderValueConverter = p => p.Speed,
                       }),
                 chartHelper.DrawBorderAsync(GpxTrack.Points.TimeOrderedPoints.Where(p => !double.IsNaN(p.Z)),
                    false, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.BorderSetting<GpxPoint>()
                    {
                        XAxisBorderValueConverter = p => p.Time,
                        YAxisBorderValueConverter = p => p.Z,
                    }),
                 chartHelper.DrawBorderAsync(points, false,
                        new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.BorderSetting<SpeedInfo>()
                        {
                            XAxisBorderValueConverter = p => p.CenterTime,
                            YAxisBorderValueConverter = p => p.Speed,
                        }));

                await Task.WhenAll(
                 chartHelper.DrawPolygonAsync(GpxTrack.Points, 1),
          chartHelper.DrawPointsAsync(points, 0, Config.Instance.Gpx_DrawPoints),
                 chartHelper.DrawLinesAsync(lines, 0));

                await chartHelper.StretchToFitAsync();
            }
            catch (Exception ex)
            {
                App.Log.Error("绘制图形失败：" + ex.Message);
            }
        }

        private void SpeedChartMouseLeave(object sender, MouseEventArgs e)
        {
            arcMap.ClearSelection();
        }

        private void MapPointSelected(object sender, GpxMapView.PointSelectedEventArgs e)
        {
            if (e.Point == null)
            {
                return;
            }
            if (arcMap.MapTapMode == GpxMapView.MapTapModes.SelectedLayer)
            {
                grdPoints.SelectedItem = e.Point;
                grdPoints.ScrollIntoView(e.Point);
            }
            else
            {
                lvwFiles.SelectedItem = e.Trajectory;
                SnakeBar.Show("已跳转到轨迹：" + e.Trajectory.FileName);
            }
            arcMap.MapTapMode = GpxMapView.MapTapModes.None;
            Cursor = Cursors.Arrow;
        }

        private async void GpxLoaded(object sender, GpxMapView.GpxLoadedEventArgs e)
        {
            if (e.Track.Length > 0)
            {
                lvwFiles.SelectedItem = e.Track[^1];
                //if (!e.Update)
                //{
                //    await ZoomToTrackAsync(0);
                //}
            }
        }

        private Gpx gpx;

        public Gpx Gpx
        {
            get => gpx;
            set => this.SetValueAndNotify(ref gpx, value, nameof(Gpx));
        }

        private GpxTrack gpxTrack;

        public GpxTrack GpxTrack
        {
            get => gpxTrack;
            set
            {
                this.SetValueAndNotify(ref gpxTrack, value, nameof(GpxTrack));
                grdLeft.IsEnabled = value != null;
            }
        }

        private void PointsGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var point = grdPoints.SelectedItem as GpxPoint;
            var points = grdPoints.SelectedItems.Cast<GpxPoint>();
            if (point != null && !double.IsNaN(point.Z) && point.Y != 0)
            {
                chartHelper.SetLine(point.Time);
                arcMap.SelectPoints(points);
            }
            else
            {
                arcMap.ClearSelection();
                chartHelper.ClearLine();
            }
        }

        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files) || files.Length == 0)
            {
                return;
            }
            bool yes = true;
            if (files.Length > 10)
            {
                yes = await CommonDialog.ShowYesNoDialogAsync("导入文件较多，是否确定导入？", $"将导入{files.Length}个文件");
            }
            if (yes)
            {
                await DoAsync(() => arcMap.LoadFilesAsync(files), "正在导入轨迹");
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

        private async void SpeedButtonClick(object sender, RoutedEventArgs e)
        {
            int? num = await CommonDialog.ShowIntInputDialogAsync("请选择单边采样率");
            if (num.HasValue)
            {
                foreach (var item in GpxTrack.Points)
                {
                    double speed = GpxTrack.Points.GetSpeed(item as GpxPoint, num.Value);
                    item.Speed = speed;
                }
            }

            var source = grdPoints.ItemsSource;
            grdPoints.ItemsSource = null;
            grdPoints.ItemsSource = source;
        }

        private async void SaveFileButtonClick(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().Add("GPX轨迹文件", "gpx").CreateSaveFileDialog().SetDefault(Gpx.Name + ".gpx").GetFilePath();
            if (path != null)
            {
                try
                {
                    File.WriteAllText(path, gpx.ToGpxXml());
                    SnakeBar.Show("导出成功");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
                }
            }
        }

        private async void OpenFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string[] files = new FileFilterCollection()
                .Add("GPX轨迹文件", "gpx")
                .CreateOpenFileDialog()
                .GetFilePaths();
            if (files != null)
            {
                await DoAsync(() => arcMap.LoadFilesAsync(files), "正在加载轨迹");
            }
        }

        #endregion 左下角按钮

        #region 文件操作

        private void ClearFileListButtonClick(object sender, RoutedEventArgs e)
        {
            Tracks.Clear();
        }

        #endregion 文件操作

        #region 点菜单

        private async void SpeedMenuItemClick(object sender, RoutedEventArgs e)
        {
            int? num = await CommonDialog.ShowIntInputDialogAsync("请选择单边采样率");
            if (num.HasValue)
            {
                double speed = GpxTrack.Points.GetSpeed(grdPoints.SelectedItem as GpxPoint, num.Value);
                await CommonDialog.ShowOkDialogAsync("速度为：" + speed.ToString("0.00") + "m/s，" + (3.6 * speed).ToString("0.00") + "km/h");
            }
        }

        private void DeletePointMenuClick(object sender, RoutedEventArgs e)
        {
            var points = grdPoints.SelectedItems.Cast<GpxPoint>().ToArray();
            if (points.Length == 0)
            {
                SnakeBar.ShowError("请先选择一个或多个点");
                return;
            }
            foreach (var point in points)
            {
                GpxTrack.Points.Remove(point);
            }
        }

        private async void UpdateTrackButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.LoadTrack(arcMap.SelectedTrack, true);
            await UpdateUI();
        }

        private void InsertPointButtonClick(object sender, RoutedEventArgs e)
        {
            var points = grdPoints.SelectedItems.Cast<GpxPoint>().ToArray();
            if (points.Length == 0)
            {
                SnakeBar.ShowError("请先选择一个或多个点");
                return;
            }
            if (points.Length > 1)
            {
                SnakeBar.ShowError("请只选择一个点");
                return;
            }
            int index = grdPoints.SelectedIndex;
            if ((sender as FrameworkElement).Tag.Equals("After"))
            {
                index++;
            }

            GpxPoint point = points[0].Clone() as GpxPoint;
            GpxTrack.Points.Insert(index, point);
            //arcMap.gpxPointAndGraphics.Add(point,)
            //arcMap.pointToTrackInfo.Add(point, arcMap.SelectedTrack);
            //arcMap.pointToTrajectoryInfo.Add(point, arcMap.SelectedTrack);
            grdPoints.SelectedItem = point;
        }

        #endregion 点菜单

        private void ResetTrackButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(lvwFiles.SelectedItem is TrackInfo track))
            {
                return;
            }

            bool smooth = Config.Instance.Gpx_AutoSmooth;
            bool height = Config.Instance.Gpx_Height;

            MenuItem menuReset = new MenuItem() { Header = "重置 - 不改变设置" };
            menuReset.Click += async (p1, p2) =>
               {
                   await arcMap.ReloadGpxAsync(track, true);
               };
            MenuItem menuResetWithSmooth = new MenuItem() { Header = "重置 - 自动平滑" };
            menuResetWithSmooth.Click += async (p1, p2) =>
            {
                Config.Instance.Gpx_AutoSmooth = true;
                try
                {
                    await arcMap.ReloadGpxAsync(track, true);
                }
                finally
                {
                    Config.Instance.Gpx_AutoSmooth = smooth;
                }
            };
            MenuItem menuResetWithoutSmooth = new MenuItem() { Header = "重置 - 不自动平滑" };
            menuResetWithoutSmooth.Click += async (p1, p2) =>
               {
                   Config.Instance.Gpx_AutoSmooth = false;
                   try
                   {
                       await arcMap.ReloadGpxAsync(track, true);
                   }
                   finally
                   {
                       Config.Instance.Gpx_AutoSmooth = smooth;
                   }
               };

            MenuItem menuResetWithHeight = new MenuItem() { Header = "重置 - 显示高度" };
            menuResetWithHeight.Click += async (p1, p2) =>
               {
                   Config.Instance.Gpx_Height = true;
                   try
                   {
                       await arcMap.ReloadGpxAsync(track, true);
                   }
                   finally
                   {
                       Config.Instance.Gpx_Height = height;
                   }
               };

            MenuItem menuResetWithoutHeight = new MenuItem() { Header = "重置 - 不显示高度" };
            menuResetWithoutHeight.Click += async (p1, p2) =>
               {
                   Config.Instance.Gpx_Height = false;
                   try
                   {
                       await arcMap.ReloadGpxAsync(track, true);
                   }
                   finally
                   {
                       Config.Instance.Gpx_Height = height;
                   }
               };

            ContextMenu menu = new ContextMenu()
            {
                PlacementTarget = sender as UIElement,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                IsOpen = true,
                Items =
                {
                    menuReset,
                    menuResetWithHeight,
                    menuResetWithoutHeight,
                    menuResetWithSmooth,
                    menuResetWithoutSmooth,
                }
            };
        }

        private void RemoveTrackFileMenuClick(object sender, RoutedEventArgs e)
        {
            ListViewItemPreviewDeleteKeyDown(null, null);
        }

        private async void LinkTrackMenuClick(object sender, RoutedEventArgs e)
        {
            TrackInfo[] tracks = lvwFiles.SelectedItems.Cast<TrackInfo>().ToArray();
            if (tracks.Length <= 1)
            {
                SnakeBar.ShowError("至少要2个轨迹才能进行连接操作");
                return;
            }

            Gpx gpx = tracks[0].Gpx.Clone() as Gpx;
            for (int i = 1; i < tracks.Length; i++)
            {
                foreach (var p in tracks[i].Track.Points)
                {
                    gpx.Tracks[0].Points.Add(p);
                }
            }
            string filePath =
                new FileFilterCollection().Add("GPX轨迹文件", "gpx")
                .CreateSaveFileDialog()
                .SetDefault(tracks[0].FileName + " - 连接.gpx")
                .GetFilePath();

            if (filePath != null)
            {
                gpx.Save(filePath);
                await arcMap.LoadGpxAsync(filePath, true);
            }
        }

        private void ArcMapTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (arcMap.MapTapMode == GpxMapView.MapTapModes.None && lvwFiles.SelectedItem != null && grdPoints.SelectedItems.Count == 1)
            {
                GpxPoint point = grdPoints.SelectedItem as GpxPoint;
                var overlay = arcMap.TapOverlay;
                if (overlay == null || !arcMap.GraphicsOverlays.Contains(overlay))
                {
                    overlay = arcMap.TapOverlay = new GraphicsOverlay();
                    arcMap.GraphicsOverlays.Add(overlay);
                    overlay.Renderer = new SimpleRenderer(new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Red, 12));
                }
                while (overlay.Graphics.Count > 0)
                {
                    overlay.Graphics.RemoveAt(overlay.Graphics.Count - 1);
                }
                MapPoint mapPoint = e.Location;
                overlay.Graphics.Add(new Graphic(mapPoint));

                ContextMenu menu = new ContextMenu()
                {
                    Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint,
                    HorizontalOffset = 20,
                    VerticalOffset = 20,
                };
                MenuItem menuSetToHere = new MenuItem() { Header = "移动点到此" };
                menuSetToHere.Click += (p1, p2) =>
                {
                    if (arcMap.gpxPointAndGraphics.ContainsKey(point))
                    {
                        double oldZ = (arcMap.gpxPointAndGraphics[point].Geometry as MapPoint).Z;
                        mapPoint = new MapPoint(mapPoint.X, mapPoint.Y, oldZ, mapPoint.SpatialReference);

                        arcMap.gpxPointAndGraphics[point].Geometry = mapPoint;
                    }
                    else
                    {
                        mapPoint = new MapPoint(mapPoint.X, mapPoint.Y, mapPoint.SpatialReference);
                    }
                    mapPoint = GeometryEngine.Project(mapPoint, SpatialReferences.Wgs84) as MapPoint;
                    // mapPoint = new MapPoint(mapPoint.X, mapPoint.Y, oldZ, mapPoint.SpatialReference);
                    point.X = mapPoint.X;
                    point.Y = mapPoint.Y;
                    //point.Z = mapPoint.Z;
                    while (overlay.Graphics.Count > 0)
                    {
                        overlay.Graphics.RemoveAt(overlay.Graphics.Count - 1);
                    }
                };
                menu.Items.Add(menuSetToHere);
                menu.IsOpen = true;
                menu.Closed += (p1, p2) =>
                  {
                      while (overlay.Graphics.Count > 0)
                      {
                          overlay.Graphics.RemoveAt(overlay.Graphics.Count - 1);
                      }
                  };
            }
        }

        private void OperationButtonClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuHeightSmooth = new MenuItem() { Header = "高度平滑" };
            menuHeightSmooth.Click += (p1, p2) => SmoothAsync(false, true);
            MenuItem menuSmooth = new MenuItem() { Header = "平滑" };
            menuSmooth.Click += (p1, p2) => SmoothAsync(true, true);
            MenuItem menuHeightOffset = new MenuItem() { Header = "高度整体偏移" };
            menuHeightOffset.Click += ElevationOffsetMenuClick;
            MenuItem menuSpeed = new MenuItem() { Header = "计算速度" };
            menuSpeed.Click += SpeedButtonClick;

            //MenuItem menuDeletePoints = new MenuItem() { "删除一个区域的所有点" };
            //menuDeletePoints.Click += DeletePointsMenuClick;

            ContextMenu menu = new ContextMenu()
            {
                PlacementTarget = sender as FrameworkElement,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                Items = { menuSmooth, menuHeightSmooth, menuHeightOffset, menuSpeed },
                IsOpen = true,
            };
        }

        private void DeletePointsMenuClick(object sender, RoutedEventArgs e)
        {
        }

        private async Task SmoothAsync(bool xy, bool z)
        {
            if (lvwFiles.SelectedItem == null)
            {
                SnakeBar.ShowError("请先选择一段轨迹！");
                return;
            }
            TrackInfo track = lvwFiles.SelectedItem as TrackInfo;
            var points = track.Track.Points;
            int count = points.Count;
            int? result = await CommonDialog.ShowIntInputDialogAsync("请输入平滑度（0~{count}）");
            if (result.HasValue)
            {
                int num = result.Value;
                if (num < 2 || num >= count)
                {
                    await CommonDialog.ShowErrorDialogAsync("输入的数值超出范围");
                    return;
                }
                if (z)
                {
                    GpxUtility.Smooth(points, num, p => p.Z, (p, v) => p.Z = v);
                }
                if (xy)
                {
                    GpxUtility.Smooth(points, num, p => p.X, (p, v) => p.X = v);
                    GpxUtility.Smooth(points, num, p => p.Y, (p, v) => p.Y = v);
                }
                UpdateTrackButtonClick(null, null);
            }
        }

        private async void ElevationOffsetMenuClick(object sender, RoutedEventArgs e)
        {
            double? num = await CommonDialog.ShowDoubleInputDialogAsync("请选择整体移动高度，向上为正（m）");
            if (num.HasValue)
            {
                GpxTrack.Points.ForEach(p => p.Z += num.Value);
            }
        }

        private async void RecoverCameraButtonClick(object sender, RoutedEventArgs e)
        {
            Camera camera = new Camera(arcMap.Camera.Location, 0, 0, 0);
            await arcMap.SetViewpointCameraAsync(camera);
        }

        private async void CaptureScreenButtonClick(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().Add("PNG图片", "png")
                .CreateSaveFileDialog()
                .SetDefault(Gpx.Name + ".png")
                .GetFilePath();
            if (path != null)
            {
                PanelExport export = new PanelExport(grd, 0, VisualTreeHelper.GetDpi(this).DpiScaleX, VisualTreeHelper.GetDpi(this).DpiScaleX);
                var bitmap = export.GetBitmap().ToBitmap();

                Graphics g = Graphics.FromImage(bitmap);
                Bitmap image = await arcMap.GetImageAsync(GeoViewHelper.GetWatermarkThickness());

                g.DrawImage(image, 0, 0, image.Width, image.Height);
                g.Flush();
                bitmap.Save(path);
            }
        }

        private async void ZoomToTrackButtonClick(object sender, RoutedEventArgs e)
        {
            await ZoomToTrackAsync();
        }

        private Task ZoomToTrackAsync(int time = 500)
        {
            if (time <= 0)
            {
                time = 0;
            }
            return arcMap.SetViewpointAsync(new Viewpoint(GpxTrack.Points.Extent), TimeSpan.FromMilliseconds(time)); ;
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e)
        {
            GpxBrowseWindow window = new GpxBrowseWindow(lvwFiles.SelectedItem as TrackInfo);
            window.Show();
        }
    }
}