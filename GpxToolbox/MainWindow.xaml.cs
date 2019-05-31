using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using FzLib.Control.Dialog;
using FzLib.DataAnalysis;
using FzLib.Extension;
using GIS.Geometry;
using GIS.IO.Gpx;
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
using static GIS.Analysis.SpeedAnalysis;
using static MapBoard.GpxToolbox.SymbolResources;
using MessageBox = FzLib.Control.Dialog.MessageBox;
using Envelope = Esri.ArcGISRuntime.Geometry.Envelope;
using FzLib.Control.Extension;
using Esri.ArcGISRuntime.Symbology;
using System.Diagnostics;
using MapBoard.Common.Dialog;
using System.Collections;
using System.Collections.Generic;

namespace MapBoard.GpxToolbox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MainWindowBase
    {
        private const string TrackFilePath = "Track.ini";
        private string[] loadNeeded = null;
        private TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint> chartHelper;
        public MainWindow(string[] load = null)
        {
            loadNeeded = load;
            InitializeComponent();
            InitializeChart();
            ListViewHelper<TrackInfo> lvwHelper = new ListViewHelper<TrackInfo>(lvwFiles);
            lvwHelper.EnableDragAndDropItem();
            //TaskDialog.DefaultOwner = this;

        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            TaskDialog.DefaultOwner = this;
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
            chartHelper.YLabelFormat = p => p + "m/s";
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
            chartHelper.LinePointEnbale = p => p.Speed > 0.2;
        }


        private void WindowClosing(object sender, CancelEventArgs e)
        {
            File.WriteAllLines(TrackFilePath, TrackInfo.Tracks.Select(p => p.FilePath).ToArray());
            TrackInfo.Tracks.Clear();
            // map.Dispose();
        }

        private void ListViewItemPreviewDeleteKeyDown(object sender, KeyEventArgs e)
        {
            foreach (var item in lvwFiles.SelectedItems.Cast<TrackInfo>().ToArray())
            {
                arcMap.GraphicsOverlays.Remove(item.Overlay);
                TrackInfo.Tracks.Remove(item);

            }
        }
        private void FileSelectionChanged(object sender, SelectionChangedEventArgs e)
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

                GIS.Geometry.Envelope extent = arcMap.SelectedTrack.Track.Points.Extent;
                var esriExtent = new Envelope(extent.XMin, extent.YMin, extent.XMax, extent.YMax, SpatialReferences.Wgs84);
                arcMap.SetViewpointAsync(new Viewpoint(esriExtent));

                Gpx = arcMap.SelectedTrack.Gpx;
                GpxTrack = arcMap.SelectedTrack.Track;
                UpdateChart();
            }

        }

        private void UpdateChart()
        {
            try
            {
                var points = GetUsableSpeeds(arcMap.SelectedTrack.Track.Points);
                var lines = GetFilteredSpeeds(arcMap.SelectedTrack.Track.Points, 20, 5);
                chartHelper.DrawAction = () =>
                {
                    chartHelper.Initialize();
                    chartHelper.DrawBorder(points, true, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.BorderSetting<SpeedInfo>()
                    {
                        XAxisBorderValueConverter = p => p.CenterTime,
                        YAxisBorderValueConverter = p => p.Speed,
                    });
                    chartHelper.DrawBorder(arcMap.SelectedTrack.Track.Points.TimeOrderedPoints.Where(p => !double.IsNaN(p.Z)), false, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint>.BorderSetting<GpxPoint>()
                    {
                        XAxisBorderValueConverter = p => p.Time,
                        YAxisBorderValueConverter = p => p.Z,
                    });
                    chartHelper.DrawPolygon(arcMap.SelectedTrack.Track.Points, 1);
                    chartHelper.DrawPoints(points, 0);
                    chartHelper.DrawLines(lines, 0);
                };
                chartHelper.DrawAction();


                var speed = arcMap.SelectedTrack.Track.AverageSpeed;

                txtSpeed.Text = speed.ToString("0.00") + "m/s    " + (speed * 3.6).ToString("0.00") + "km/h";
                txtDistance.Text = (arcMap.SelectedTrack.Track.Distance / 1000).ToString("0.00") + "km";

                var movingSpeed = arcMap.SelectedTrack.Track.GetMovingAverageSpeed();
                txtMovingSpeed.Text = movingSpeed.ToString("0.00") + "m/s    " + (movingSpeed * 3.6).ToString("0.00") + "km/h";
                txtMovingTime.Text = arcMap.SelectedTrack.Track.GetMovingTime().ToString();
                txtMovingTime.Text = arcMap.SelectedTrack.Track.GetMovingTime().ToString();

                var maxSpeed = arcMap.SelectedTrack.Track.GetMaxSpeed();
                txtMaxSpeed.Text = maxSpeed.ToString("0.00") + "m/s    " + (maxSpeed * 3.6).ToString("0.00") + "km/h";
            }
            catch (Exception ex)
            {

            }
        }
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (loadNeeded != null)
            {
                arcMap.LoadFiles(loadNeeded);
            }
            else if (File.Exists(TrackFilePath))
            {
                string[] files = File.ReadAllLines(TrackFilePath);
                arcMap.LoadFiles(files);
            }

        }

        private void SpeedChartMouseLeave(object sender, MouseEventArgs e)
        {
            arcMap.ClearSelection();
            //Debug.WriteLine("Leave");
        }

        private void MapPointSelected(object sender, ArcMapView.PointSelectedEventArgs e)
        {
            if (e.Point == null)
            {
                return;
            }
            if (arcMap.MapTapMode == ArcMapView.MapTapModes.SelectedLayer)
            {
                grdPoints.SelectedItem = e.Point;
                grdPoints.ScrollIntoView(e.Point);
            }
            else
            {
                lvwFiles.SelectedItem = e.Trajectory;
                SnakeBar.Show("已跳转到轨迹：" + e.Trajectory.FileName);
            }
            arcMap.MapTapMode = ArcMapView.MapTapModes.None;
            Cursor = Cursors.Arrow;
        }

        private void GpxLoaded(object sender, ArcMapView.GpxLoadedEventArgs e)
        {
            if (e.Track.Length > 0)
            {
                lvwFiles.SelectedItem = e.Track[e.Track.Length - 1];
            }
        }

        private Gpx gpx;

        public Gpx Gpx
        {
            get => gpx;
            set => SetValueAndNotify(ref gpx, value, nameof(Gpx));
        }
        private GpxTrack gpxTrack;

        public GpxTrack GpxTrack
        {
            get => gpxTrack;
            set
            {
                SetValueAndNotify(ref gpxTrack, value, nameof(GpxTrack));
                grdLeft.IsEnabled = value != null;
            }
        }

        private void PointsGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var point = grdPoints.SelectedItem as GpxPoint;
            var points = grdPoints.SelectedItems.Cast<GpxPoint>();
            if (point != null && !double.IsNaN(point.Z) && point.Latitude != 0)
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

        private void SpeedButtonClick(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请选择单边采样率：", out string result, null, "3", "[1-9]", false, this))
            {
                if (int.TryParse(result, out int intResult))
                {
                    foreach (var item in GpxTrack.Points)
                    {
                        double speed = GpxTrack.Points.GetSpeed(item as GpxPoint, intResult);
                        item.Speed = speed;
                    }
                }
            }

            var source = grdPoints.ItemsSource;
            grdPoints.ItemsSource = null;
            grdPoints.ItemsSource = source;
        }

        private void SaveFileButtonClick(object sender, RoutedEventArgs e)
        {
            string path = FileSystemDialog.GetSaveFile(new (string, string)[] { ("GPX轨迹文件", "gpx") }, false, false, Gpx.Name + ".gpx");
            if (path != null)
            {
                try
                {
                    File.WriteAllText(path, gpx.ToGpxXml());
                    SnakeBar.Show("导出成功", this);
                }
                catch (Exception ex)
                {
                    SnakeBar.ShowException(ex, "导出失败");
                }
            }
        }

        private void OpenFilesButtonClick(object sender, RoutedEventArgs e)
        {
            string[] files = FileSystemDialog.GetOpenFiles(new (string, string)[] { ("GPX轨迹文件", "gpx") }, true, true);
            if (files != null)
            {
                arcMap.LoadFiles(files);
            }
        }

        #endregion


        #region 文件操作

        private void ClearFileListButtonClick(object sender, RoutedEventArgs e)
        {
            TrackInfo.Tracks.Clear();
        }
        #endregion

        #region 点菜单

        private void SpeedMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请选择单边采样率：", out string result, null, "3", "[1-9]", false, this))
            {
                if (int.TryParse(result, out int intResult))
                {
                    double speed = GpxTrack.Points.GetSpeed(grdPoints.SelectedItem as GpxPoint, intResult);
                    MessageBox.ShowPrompt("速度为：" + speed.ToString("0.00") + "m/s，" + (3.6 * speed).ToString("0.00") + "km/h");
                }
            }
        }

        private void DeletePointMenuClick(object sender, RoutedEventArgs e)
        {
            var points = grdPoints.SelectedItems.Cast<GpxPoint>().ToArray();
            if (points.Length == 0)
            {
                SnakeBar.ShowError("请先选择一个或多个点", this);
                return;
            }
            foreach (var point in points)
            {
                GpxTrack.Points.Remove(point);
            }
        }

        private void UpdateTrackButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.LoadTrack(arcMap.SelectedTrack, true);
            UpdateChart();
        }

        private void InsertPointButtonClick(object sender, RoutedEventArgs e)
        {
            var points = grdPoints.SelectedItems.Cast<GpxPoint>().ToArray();
            if (points.Length == 0)
            {
                SnakeBar.ShowError("请先选择一个或多个点", this);
                return;
            }
            if (points.Length > 1)
            {
                SnakeBar.ShowError("请只选择一个点", this);
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
        #endregion

        private async void ResetTrackButtonClick(object sender, RoutedEventArgs e)
        {
            int index = lvwFiles.SelectedIndex;
            if (index == -1)
            {
                return;
            }
            TrackInfo track = lvwFiles.SelectedItem as TrackInfo;

            string filePath = track.FilePath;

            //arcMap.GraphicsOverlays.Remove(track.Overlay);
            TrackInfo.Tracks.Remove(track);

            await arcMap.LoadGpx(filePath, true);
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


            Gpx gpx = tracks[0].Gpx.Clone();
            for (int i = 1; i < tracks.Length; i++)
            {
                foreach (var p in tracks[i].Track.Points)
                {
                    gpx.Tracks[0].Points.Add(p);
                }
            }
            string filePath = FileSystemDialog.GetSaveFile(new (string, string)[] { ("GPX轨迹文件", "gpx") }, false, true, tracks[0].FileName + " - 连接.gpx");
            if (filePath != null)
            {
                gpx.Save(filePath);
                await arcMap.LoadGpx(filePath, true);
            }

        }

        private void ArcMapTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (arcMap.MapTapMode == ArcMapView.MapTapModes.None && lvwFiles.SelectedItem != null && grdPoints.SelectedItems.Count == 1)
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
            menuHeightSmooth.Click += (p1,p2)=>Smooth(false,true);
            MenuItem menuSmooth = new MenuItem() { Header = "平滑" };
            menuSmooth.Click += (p1,p2)=>Smooth(true,true);
            MenuItem menuHeightOffset = new MenuItem() { Header = "高度整体偏移" };
            menuHeightOffset.Click += ElevationOffsetMenuClick;

            //MenuItem menuDeletePoints = new MenuItem() { "删除一个区域的所有点" };
            //menuDeletePoints.Click += DeletePointsMenuClick;

            ContextMenu menu = new ContextMenu()
            {
                PlacementTarget = sender as FrameworkElement,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                Items = { menuSmooth,menuHeightSmooth, menuHeightOffset },
                IsOpen = true,
            };

        }

        private void DeletePointsMenuClick(object sender, RoutedEventArgs e)
        {

        }



        private void Smooth(bool xy,bool z)
        {
            if (lvwFiles.SelectedItem == null)
            {
                SnakeBar.ShowError("请先选择一段轨迹！");
                return;
            }
            TrackInfo track = lvwFiles.SelectedItem as TrackInfo;
            var points = track.Track.Points;
            int count = points.Count;
            NumberInputDialog dialog = new NumberInputDialog($"请输入平滑度（0~{count}）") { Integer = true };
            if (dialog.ShowDialog() == true)
            {
                int num = dialog.IntNumber;
                if (num < 2 || num >= count)
                {
                    TaskDialog.ShowError("输入的数值超出范围");
                    return;
                }
                if (z)
                {
                 GpxHelper.   Smooth(points, num, p => p.Z, (p, v) => p.Z = v);
                }
                if(xy)
                {
                    GpxHelper.Smooth(points, num, p => p.X, (p, v) => p.X = v);
                    GpxHelper.Smooth(points, num, p => p.Y, (p, v) => p.Y = v);
                }
                UpdateTrackButtonClick(null, null);
            }
        }


        private void ElevationOffsetMenuClick(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请输入偏移值：", out string result, null, "", @"^[0-9]{0,5}(\.[0-9]+)?$", false, this))
            {
                if (double.TryParse(result, out double num))
                {
                    foreach (var point in GpxTrack.Points)
                    {
                        point.Z += num;
                    }
                    var temp = GpxTrack;
                    GpxTrack = null;
                    GpxTrack = temp;
                }
                else
                {
                    SnakeBar.ShowError("输入的不是数字");
                }
            }
        }

    }

    public class SpeedValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "";
            }
            double speed = (double)value;
            return speed.ToString("0.00") + "m/s    " + (3.6 * speed).ToString("0.00") + "km/h";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
