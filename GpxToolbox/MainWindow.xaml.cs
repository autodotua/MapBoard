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

namespace MapBoard.GpxToolbox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MainWindowBase
    {
        private const string TrackFilePath = "Track.ini";
        private TimeBasedChartHelper<SpeedInfo, SpeedInfo, GpxPoint> chartHelper;
        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();
            ListViewHelper<TrackInfo> lvwHelper = new ListViewHelper<TrackInfo>(lvwFiles);
            lvwHelper.EnableDragAndDropItem();
            //TaskDialog.DefaultOwner = this;
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

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if(files==null || files.Length==0)
            {
                return;
            }
            bool yes = true;
            if (files.Length > 10)
            {
                yes = false;
                yes = TaskDialog.ShowWithYesNoButtons("导入文件较多，是否确定导入？", $"导入{files.Length}个文件") == true;
            }
            if (yes)
            {
                arcMap.LoadFiles(files);
            }
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
                arcMap.SelectedTrack.Overlay.Renderer = GetNormalOverlayRenderer();
            }
            if (lvwFiles.SelectedItem == null)
            {
                arcMap.SelectedTrack = null;
                return;
            }

            if (lvwFiles.SelectedItems.Count > 1)
            {
                arcMap.SelectedTrack = null;
                Gpx = null;
                GpxTrack = null;
            }
            else
            {
                arcMap.SelectedTrack = lvwFiles.SelectedItem as TrackInfo;

                arcMap.SelectedTrack.Overlay.Renderer = GetCurrentOverlayRenderer();
                GIS.Geometry.Envelope extent = arcMap.SelectedTrack.Track.Points.Extent;
                var esriExtent = new Envelope(extent.XMin, extent.YMin, extent.XMax, extent.YMax, SpatialReferences.Wgs84);
                arcMap.SetViewpointGeometryAsync(esriExtent);
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

                Gpx = arcMap.SelectedTrack.Gpx;
                GpxTrack = arcMap.SelectedTrack.Track;

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
            if (File.Exists(TrackFilePath))
            {
                string[] files = File.ReadAllLines(TrackFilePath);
                arcMap.LoadFiles(files);
            }
        }

        private void SpeedChartMouseLeave(object sender, MouseEventArgs e)
        {
            arcMap.ClearSelection();
        }

        private void MapPointSelected(object sender, ArcMapView.PointSelectedEventArgs e)
        {
            if (e.Point == null)
            {
                return;
            }
            if (arcMap.SelectionMode == ArcMapView.SelectionModes.SelectedLayer)
            {
                pointsGrid.SelectedItem = e.Point;
                pointsGrid.ScrollIntoView(e.Point);
            }
            else
            {
                lvwFiles.SelectedItem = e.Trajectory;
                SnakeBar.Show("已跳转到轨迹：" + e.Trajectory.FileName);
            }
            arcMap.SelectionMode = ArcMapView.SelectionModes.None;
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
            var point = pointsGrid.SelectedItem as GpxPoint;
            if (point!=null  &&!double.IsNaN( point.Z )&& point.Latitude != 0)
            {

                chartHelper.SetLine(point.Time);
                arcMap.SelectPointTo(point);
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
            arcMap.SelectionMode = ArcMapView.SelectionModes.SelectedLayer;
            Cursor = Cursor == Cursors.Help ? Cursors.Arrow : Cursors.Help;
        }
        private void IdentifyAllButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.SelectionMode = ArcMapView.SelectionModes.AllLayers;
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

            var source = pointsGrid.ItemsSource;
            pointsGrid.ItemsSource = null;
            pointsGrid.ItemsSource = source;
        }

        private void SaveFileButtonClick(object sender, RoutedEventArgs e)
        {
            string path = FileSystemDialog.GetSaveFile(new (string, string)[] { ("GPX轨迹文件", "gpx") }, false, false, Gpx.Name + ".gpx");
            if (path != null)
            {
                try
                {
                    File.WriteAllText(path, gpx.ToGpxXml());
                    SnakeBar.Show("导出成功");
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

        private void ElevationOffsetButtonClick(object sender, RoutedEventArgs e)
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
        #endregion


        #region 文件操作

        private void ClearFileListButtonClick(object sender, RoutedEventArgs e)
        {
            arcMap.GraphicsOverlays.Clear();
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
                    double speed = GpxTrack.Points.GetSpeed(pointsGrid.SelectedItem as GpxPoint, intResult);
                    MessageBox.ShowPrompt("速度为：" + speed.ToString("0.00") + "m/s，" + (3.6 * speed).ToString("0.00") + "km/h");
                }
            }
        }

        private void DeletePointMenuClick(object sender, RoutedEventArgs e)
        {
            var points = pointsGrid.SelectedItems.Cast<GpxPoint>().ToArray();
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
            var points = pointsGrid.SelectedItems.Cast<GpxPoint>().ToArray();
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
            int index = pointsGrid.SelectedIndex;
            if ((sender as FrameworkElement).Tag.Equals("After"))
            {
                index++;
            }

            GpxPoint point = points[0].Clone() as GpxPoint;
            GpxTrack.Points.Insert(index, point);
            arcMap.pointToTrackInfo.Add(point, arcMap.SelectedTrack);
            //arcMap.pointToTrajectoryInfo.Add(point, arcMap.SelectedTrack);
            pointsGrid.SelectedItem = point;
        } 
        #endregion

        private void ResetTrackButtonClick(object sender, RoutedEventArgs e)
        {
            int index = lvwFiles.SelectedIndex;
            if(index==-1)
            {
                return;
            }
            TrackInfo track = lvwFiles.SelectedItem as TrackInfo;

            string filePath = track.FilePath;

            arcMap.GraphicsOverlays.Remove(track.Overlay);
            TrackInfo.Tracks.Remove(track);

            arcMap.LoadGpx(filePath, true);
        }

        private void RemoveTrackFileMenuClick(object sender, RoutedEventArgs e)
        {
            ListViewItemPreviewDeleteKeyDown(null, null);
        }

        private void LinkTrackMenuClick(object sender, RoutedEventArgs e)
        {
            TrackInfo[] tracks = lvwFiles.SelectedItems.Cast<TrackInfo>().ToArray();
            if (tracks.Length<=1)
            {
                SnakeBar.ShowError("至少要2个轨迹才能进行连接操作");
                return;
            }


            Gpx gpx = tracks[0].Gpx.Clone();
            for(int i=1;i<tracks.Length;i++)
            {
                foreach (var p in tracks[i].Track.Points)
                {
                    gpx.Tracks[0].Points.Add(p);
                }
            }
            string filePath = FileSystemDialog.GetSaveFile(new (string, string)[] { ("GPX轨迹文件", "gpx") }, false, true, tracks[0].FileName + " - 连接.gpx");
            if(filePath!=null)
            {
                gpx.Save(filePath);
                arcMap.LoadGpx(filePath, true);
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
