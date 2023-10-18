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
using static MapBoard.UI.GpxToolbox.GpxSymbolResources;
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
using MapBoard.IO;

namespace MapBoard.UI.GpxToolbox
{
    /// <summary>
    /// GPX工具箱
    /// </summary>
    public partial class GpxWindow : MainWindowBase
    {
        private TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint> chartHelper;

        public GpxWindow()
        {
            InitializeComponent();
            arcMap.Tracks = Tracks;
            InitializeChart();
            ListViewHelper<TrackInfo> lvwHelper = new ListViewHelper<TrackInfo>(lvwFiles);
            lvwHelper.EnableDragAndDropItem();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        protected override async Task InitializeAsync()
        {
            if (LoadFiles != null)
            {
                await arcMap.LoadFilesAsync(LoadFiles);
            }
            else if (File.Exists(FolderPaths.TrackHistoryPath))
            {
                string[] files = await File.ReadAllLinesAsync(FolderPaths.TrackHistoryPath);
                await DoAsync(() => arcMap.LoadFilesAsync(files), "正在导入轨迹");
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
                App.Log.Error("绘制GPX图表失败", ex);
            }
        }

        /// <summary>
        /// GPX加载完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GpxLoaded(object sender, GpxMapView.GpxLoadedEventArgs e)
        {
            if (e.Track.Length > 0)
            {
                lvwFiles.SelectedItem = e.Track[^1];
            }
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            await File.WriteAllLinesAsync(FolderPaths.TrackHistoryPath, Tracks.Select(p => p.FilePath).ToArray());
            Tracks.Clear();
        }

        #region GPX和轨迹属性
        /// <summary>
        /// GPX对象
        /// </summary>
        public Gpx Gpx { get; set; }

        /// <summary>
        /// GPX轨迹对象
        /// </summary>
        public GpxTrack GpxTrack { get; set; }

        /// <summary>
        /// 启动后需要加载的文件
        /// </summary>
        public string[] LoadFiles { get; set; } = null;

        /// <summary>
        /// 所有轨迹
        /// </summary>
        public ObservableCollection<TrackInfo> Tracks { get; } = new ObservableCollection<TrackInfo>();

        #endregion

        #region 轨迹信息
        /// <summary>
        /// 距离
        /// </summary>
        public string DistanceText { get; set; }

        /// <summary>
        /// 最大速度
        /// </summary>
        public string MaxSpeedText { get; set; }

        /// <summary>
        /// 移动速度
        /// </summary>
        public string MovingSpeedText { get; set; }

        /// <summary>
        /// 移动时间
        /// </summary>
        public string MovingTimeText { get; set; }

        /// <summary>
        /// 速度
        /// </summary>
        public string SpeedText { get; set; }

        #endregion

        #region 轨迹操作

        /// <summary>
        /// 单击缩放到轨迹按钮
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private Task ZoomToTrackAsync(int time = 500)
        {
            if (time <= 0)
            {
                time = 0;
            }
            return arcMap.SetViewpointAsync(new Viewpoint(GpxTrack.Points.Extent), TimeSpan.FromMilliseconds(time)); ;
        }

        /// <summary>
        /// 单击截图按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CaptureScreenButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().Add("PNG图片", "png")
                .CreateSaveFileDialog()
                .SetDefault(Gpx.Name + ".png")
                .SetParent(this)
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

        /// <summary>
        /// 单击高程偏移按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ElevationOffsetMenu_Click(object sender, RoutedEventArgs e)
        {
            double? num = await CommonDialog.ShowDoubleInputDialogAsync("请选择整体移动高度，向上为正（m）");
            if (num.HasValue)
            {
                GpxTrack.Points.ForEach(p => p.Z += num.Value);
            }
        }

        /// <summary>
        /// 单击识别所有轨迹按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdentifyAllButton_Click(object sender, RoutedEventArgs e)
        {
            arcMap.MapTapMode = GpxMapView.MapTapModes.AllLayers;
            Cursor = Cursor == Cursors.Help ? Cursors.Arrow : Cursors.Help;
        }

        /// <summary>
        /// 单击识别轨迹中的点按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            arcMap.MapTapMode = GpxMapView.MapTapModes.SelectedLayer;
            Cursor = Cursor == Cursors.Help ? Cursors.Arrow : Cursors.Help;
        }

        /// <summary>
        /// 单击打开文件按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenFilesButton_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// 单击操作按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OperationButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuHeightSmooth = new MenuItem() { Header = "高度平滑" };
            menuHeightSmooth.Click += (p1, p2) => SmoothAsync(false, true);
            MenuItem menuSmooth = new MenuItem() { Header = "平滑" };
            menuSmooth.Click += (p1, p2) => SmoothAsync(true, true);
            MenuItem menuHeightOffset = new MenuItem() { Header = "高度整体偏移" };
            menuHeightOffset.Click += ElevationOffsetMenu_Click;
            MenuItem menuSpeed = new MenuItem() { Header = "计算速度" };
            menuSpeed.Click += SpeedButton_Click;

            //MenuItem menuDeletePoints = new MenuItem() { "删除一个区域的所有点" };
            //menuDeletePoints.Click += DeletePointsMenu_Click;

            ContextMenu menu = new ContextMenu()
            {
                PlacementTarget = sender as FrameworkElement,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                Items = { menuSmooth, menuHeightSmooth, menuHeightOffset, menuSpeed },
                IsOpen = true,
            };
        }

        /// <summary>
        /// 单击恢复相机视角按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RecoverCameraButton_Click(object sender, RoutedEventArgs e)
        {
            Camera camera = new Camera(arcMap.Camera.Location, 0, 0, 0);
            await arcMap.SetViewpointCameraAsync(camera);
        }

        /// <summary>
        /// 单击重置轨迹按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetTrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (lvwFiles.SelectedItem is not TrackInfo track)
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

        /// <summary>
        /// 单击保存按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection()
                .Add("GPX轨迹文件", "gpx")
                .CreateSaveFileDialog()
                .SetDefault(Gpx.Name + ".gpx")
                .SetParent(this)
                .GetFilePath();
            if (path != null)
            {
                try
                {
                    await File.WriteAllTextAsync(path, Gpx.ToGpxXml());
                    SnakeBar.Show("导出成功");
                }
                catch (Exception ex)
                {
                    App.Log.Error("导出失败", ex);
                    await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
                }
            }
        }

        /// <summary>
        /// 平滑轨迹
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="z"></param>
        /// <returns></returns>
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
                if (z && points.All(p=>p.Z.HasValue))
                {
                    GpxUtility.Smooth(points, num, p => p.Z.Value, (p, v) => p.Z = v);
                }
                if (xy)
                {
                    GpxUtility.Smooth(points, num, p => p.X, (p, v) => p.X = v);
                    GpxUtility.Smooth(points, num, p => p.Y, (p, v) => p.Y = v);
                }
                UpdateTrackButton_Click(null, null);
            }
        }

        /// <summary>
        /// 单击计算速度按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SpeedButton_Click(object sender, RoutedEventArgs e)
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

        #endregion 左下角按钮

        #region 文件操作
        /// <summary>
        /// 文件拖放，加载文件
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files) || files.Length == 0)
            {
                return;
            }

            List<string> fileList = new List<string>();
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    fileList.Add(file);
                }
                else if (Directory.Exists(file))
                {
                    fileList.AddRange(Directory.EnumerateFiles(file, "*.gpx", SearchOption.AllDirectories));
                }
            }
            bool yes = true;
            if (fileList.Count > 10)
            {
                yes = await CommonDialog.ShowYesNoDialogAsync("导入文件较多，是否确定导入？", $"将导入{fileList.Count}个文件");
            }
            if (yes)
            {
                await DoAsync(() => arcMap.LoadFilesAsync(fileList), "正在导入轨迹");
            }
        }

        /// <summary>
        /// 单击清除轨迹按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearFileListButton_Click(object sender, RoutedEventArgs e)
        {
            Tracks.Clear();
        }

        /// <summary>
        /// 选择的GPX文件是否全部点都提供了高度和时间信息
        /// </summary>
        private bool hasZAndTimeInfo;

        /// <summary>
        /// 选择的的文件改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void File_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
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

                    hasZAndTimeInfo = GpxTrack.Points.All(p => p.Z.HasValue && p.Time.HasValue);

                    if (hasZAndTimeInfo)
                    {
                        await Task.WhenAll(ZoomToTrackAsync(), UpdateUI());
                    }
                    else
                    {
                        await ZoomToTrackAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("选择Gpx文件时出现错误", ex);
            }
        }

        /// <summary>
        /// 右键文件菜单项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilesList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu menu = new ContextMenu
            {
                PlacementTarget = sender as UIElement
            };

            if (lvwFiles.SelectedItems.Count > 0)
            {
                var menuRemove = new MenuItem() { Header = "移除" };
                menuRemove.Click += (s, e) => RemoveSelectedGpxs();
                menu.Items.Add(menuRemove);

                if (lvwFiles.SelectedItems.Count > 1)
                {
                    var menuLink = new MenuItem() { Header = "连接" };
                    menuLink.Click += LinkTrackMenu_Click;
                    menu.Items.Add(menuLink);
                }
            }
            if (GpxTrack != null)
            {
                var menuZoom = new MenuItem() { Header = "缩放到轨迹" };
                menuZoom.Click += async (s, e) => await ZoomToTrackAsync();
                menu.Items.Add(menuZoom);

                var menuBrowse = new MenuItem() { Header = "游览" };
                menuBrowse.Click += (s, e) => new GpxBrowseWindow(lvwFiles.SelectedItem as TrackInfo).Show();
                menu.Items.Add(menuBrowse);
            }
            menu.IsOpen = true;
        }

        /// <summary>
        /// 单击连接轨迹菜单项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LinkTrackMenu_Click(object sender, RoutedEventArgs e)
        {
            TrackInfo[] tracks = lvwFiles.SelectedItems.Cast<TrackInfo>().OrderBy(p => p.Track.Points.FirstOrDefault()?.Time ?? DateTime.MaxValue).ToArray();
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
                .SetParent(this)
                .GetFilePath();

            if (filePath != null)
            {
                gpx.Save(filePath);
                await arcMap.LoadGpxAsync(filePath, true);
            }
        }

        /// <summary>
        /// 选择文件后单击Delete按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListViewItemPreviewDeleteKeyDown(object sender, KeyEventArgs e)
        {
            if (e == null || e.Key == Key.Delete)
            {
                RemoveSelectedGpxs();
            }
        }

        /// <summary>
        /// 移除选中的GPX
        /// </summary>
        private void RemoveSelectedGpxs()
        {
            foreach (var item in lvwFiles.SelectedItems.Cast<TrackInfo>().ToList())
            {
                arcMap.GraphicsOverlays.Remove(item.Overlay);
                Tracks.Remove(item);
            }
        }
        #endregion 文件操作

        #region 轨迹点

        /// <summary>
        /// 单击地图，可以将选中的点移动到鼠标位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArcMapTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (arcMap.MapTapMode == GpxMapView.MapTapModes.None && lvwFiles.SelectedItem != null
                && grdPoints.SelectedItems.Count == 1)
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

        /// <summary>
        /// 单击删除点菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeletePointMenu_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// 单击插入点菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InsertPointButton_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// 选择了一个地图点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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


        /// <summary>
        /// 某个点被选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PointsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var points = grdPoints.SelectedItems.Cast<GpxPoint>();
            if (grdPoints.SelectedItem is GpxPoint point && !double.IsNaN(point.Z.Value) && point.Y != 0)
            {
                chartHelper.SetLine(point.Time.Value);
                arcMap.SelectPoints(points);
            }
            else
            {
                arcMap.ClearSelection();
                chartHelper.ClearLine();
            }
        }

        /// <summary>
        /// 单击速度菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SpeedMenuItemClick(object sender, RoutedEventArgs e)
        {
            int? num = await CommonDialog.ShowIntInputDialogAsync("请选择单边采样率");
            if (num.HasValue)
            {
                double speed = GpxTrack.Points.GetSpeed(grdPoints.SelectedItem as GpxPoint, num.Value);
                await CommonDialog.ShowOkDialogAsync("速度为：" + speed.ToString("0.00") + "m/s，" + (3.6 * speed).ToString("0.00") + "km/h");
            }
        }

        /// <summary>
        /// 单击更新轨迹按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateTrackButton_Click(object sender, RoutedEventArgs e)
        {
            arcMap.LoadTrack(arcMap.SelectedTrack, true);
            await UpdateUI();
        }

        #endregion 点菜单

        #region 图表

        /// <summary>
        /// 绘制图表
        /// </summary>
        /// <param name="points"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        private async Task DrawChartAsync(IEnumerable<SpeedInfo> points, IReadOnlyList<SpeedInfo> lines)
        {
            try
            {
                //1、初始化
                chartHelper.Initialize();

                //2、绘制坐标轴和网格
                await Task.WhenAll(
                 chartHelper.DrawAxisAsync(lines, true,
                       new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.CoordinateSystemSetting<SpeedInfo>()
                       {
                           XAxisValueConverter = p => p.CenterTime,
                           YAxisValueConverter = p => p.Speed,
                       }),

                 chartHelper.DrawAxisAsync(GpxTrack.Points.TimeOrderedPoints.Where(p => !double.IsNaN(p.Z.Value)),
                    false, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.CoordinateSystemSetting<GpxPoint>()
                    {
                        XAxisValueConverter = p => p.Time.Value,
                        YAxisValueConverter = p => p.Z.Value,
                    }),
                 chartHelper.DrawAxisAsync(points, false,
                        new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.CoordinateSystemSetting<SpeedInfo>()
                        {
                            XAxisValueConverter = p => p.CenterTime,
                            YAxisValueConverter = p => p.Speed,
                        }));

                //3、绘制数据
                await Task.WhenAll(
                 chartHelper.DrawPolygonAsync(GpxTrack.Points, 1),
          chartHelper.DrawPointsAsync(points, 0, Config.Instance.Gpx_DrawPoints),
                 chartHelper.DrawLinesAsync(lines, 0));

                //4、重新排版
                await chartHelper.StretchToFitAsync();
            }
            catch (Exception ex)
            {
                App.Log.Error("绘制图形失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 初始化图表，设置<see cref="TimeBasedChartHelper{TPoint, TLine, TPolygon}"/>的各项属性
        /// </summary>
        private void InitializeChart()
        {
            chartHelper = new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>(speedChart);

            chartHelper.XAxisLineValueConverter = p => p.CenterTime;
            chartHelper.XAxisPointValueConverter = p => p.CenterTime;
            chartHelper.XAxisPolygonValueConverter = p => p.Time.Value;

            chartHelper.YAxisPointValueConverter = p => p.Speed;
            chartHelper.YAxisLineValueConverter = p => p.Speed;
            chartHelper.YAxisPolygonValueConverter = p => p.Z.Value;
            chartHelper.XLabelFormat = p => p.ToString("HH:mm");
            chartHelper.YLabelFormat = p => $"{p}m/s {(p * 3.6).ToString(".0")}km/h";
            chartHelper.ToolTipConverter = p =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(p.CenterTime.ToString("HH:mm:ss"));
                sb.Append(p.Speed.ToString("0.00")).AppendLine("m/s");
                sb.Append((3.6 * p.Speed).ToString("0.00")).AppendLine("km/h");
                if (!double.IsNaN(p.RelatedPoints[0].Z.Value) && !double.IsNaN(p.RelatedPoints[1].Z.Value))
                {
                    sb.Append(((p.RelatedPoints[0].Z + p.RelatedPoints[1].Z) / 2).Value.ToString("0.00") + "m");
                }
                return sb.ToString();
            };

            chartHelper.MouseOverPoint += (p1, p2) =>
            {
                //arcMap.ClearSelection();
                arcMap.SelectPointTo(p2.Item.RelatedPoints[0]);
            };
            chartHelper.LinePointEnable = (p1, p2) => (p2.CenterTime - p1.CenterTime) < TimeSpan.FromSeconds(200);
        }

        /// <summary>
        /// 鼠标移开速度图表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpeedChartMouseLeave(object sender, MouseEventArgs e)
        {
            arcMap.ClearSelection();
        }

        #endregion
    }
}