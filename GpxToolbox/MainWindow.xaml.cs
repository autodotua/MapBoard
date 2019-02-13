using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using FzLib.Control.Dialog;
using FzLib.DataAnalysis;
using FzLib.Extension;
using FzLib.Geography.Analysis;
using FzLib.Geography.Coordinate;
using FzLib.Geography.Format;
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
using static FzLib.Geography.Analysis.SpeedAnalysis;
using static MapBoard.GpxToolbox.SymbolResources;
using MessageBox = FzLib.Control.Dialog.MessageBox;

namespace MapBoard.GpxToolbox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MainWindowBase
    {
        private const string TrajectoriesFilePath = "Trajectories.ini";
        private TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint> chartHelper;
        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();

            //TaskDialog.DefaultOwner = this;
        }

        private void InitializeChart()
        {
            chartHelper = new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint>(speedChart);

            chartHelper.XAxisLineValueConverter = p => p.CenterTime;
            chartHelper.XAxisPointValueConverter = p => p.CenterTime;
            chartHelper.XAxisPolygonValueConverter = p => p.Time.Value;

            chartHelper.YAxisPointValueConverter = p => p.Speed;
            chartHelper.YAxisLineValueConverter = p => p.Speed;
            chartHelper.YAxisPolygonValueConverter = p => p.Altitude ?? double.NaN;
            chartHelper.XLabelFormat = p => p.ToString("HH:mm");
            chartHelper.YLabelFormat = p => p + "m/s";
            chartHelper.ToolTipConverter = p =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(p.CenterTime.ToString("HH:mm:ss"));
                sb.Append(p.Speed.ToString("0.00")).AppendLine("m/s");
                sb.Append((3.6 * p.Speed).ToString("0.00")).AppendLine("km/h");
                if (p.RelatedPoints[0].Altitude.HasValue && p.RelatedPoints[1].Altitude.HasValue)
                {
                    sb.Append(((p.RelatedPoints[0].Altitude + p.RelatedPoints[1].Altitude) / 2).Value.ToString("0.00") + "m");
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

        private void StartOffset(bool north, double value)
        {
            //stop = false;
            //while(!stop)
            //{
            //    if(north)
            //    {
            //        map.OffsetNorth += value;
            //    }
            //    else
            //    {
            //        map.OffsetEast += value;
            //    }
            //    await Task.Delay(20);
            //}
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
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
            File.WriteAllLines(TrajectoriesFilePath, TrajectoryInfo.Trajectories.Select(p => p.FilePath));
            // map.Dispose();
        }

        private void ListViewItemPreviewDeleteKeyDown(object sender, KeyEventArgs e)
        {
            foreach (var item in lvwFiles.SelectedItems.Cast<TrajectoryInfo>().ToArray())
            {
                arcMap.GraphicsOverlays.Remove(item.Overlay);
                TrajectoryInfo.Trajectories.Remove(item);

            }
        }
        private void FileSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (arcMap.SelectedTrajectorie != null)
            {
                arcMap.SelectedTrajectorie.Overlay.Renderer = GetNormalOverlayRenderer();
            }
            if (lvwFiles.SelectedItem == null)
            {
                arcMap.SelectedTrajectorie = null;
                return;
            }

            if (lvwFiles.SelectedItems.Count > 1)
            {
                arcMap.SelectedTrajectorie = null;
                Gpx = null;
                GpxTrack = null;
            }
            else
            {
                arcMap.SelectedTrajectorie = lvwFiles.SelectedItem as TrajectoryInfo;

                arcMap.SelectedTrajectorie.Overlay.Renderer = GetCurrentOverlayRenderer();

                arcMap.SetViewpointGeometryAsync(arcMap.SelectedTrajectorie.Overlay.Extent);
                UpdateChart();

            }

        }

        private void UpdateChart()
        {
            var points = GetUsableSpeeds(arcMap.SelectedTrajectorie.Track.Points);
            var lines = GetFilteredSpeeds(arcMap.SelectedTrajectorie.Track.Points, 20, 5);
            chartHelper.DrawAction = () =>
            {
                chartHelper.Initialize();
                chartHelper.DrawBorder(points, true, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint>.BorderSetting<SpeedInfo>()
                {
                    XAxisBorderValueConverter = p => p.CenterTime,
                    YAxisBorderValueConverter = p => p.Speed,
                });
                chartHelper.DrawBorder(arcMap.SelectedTrajectorie.Track.Points.TimeOrderedPoints.Where(p => p.Altitude.HasValue), false, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint>.BorderSetting<GeoPoint>()
                {
                    XAxisBorderValueConverter = p => p.Time.Value,
                    YAxisBorderValueConverter = p => p.Altitude.Value,
                });
                chartHelper.DrawPolygon(arcMap.SelectedTrajectorie.Track.Points, 1);
                chartHelper.DrawPoints(points, 0);
                chartHelper.DrawLines(lines, 0);
            };
            chartHelper.DrawAction();

            Gpx = arcMap.SelectedTrajectorie.Gpx;
            GpxTrack = arcMap.SelectedTrajectorie.Track;

            var speed = arcMap.SelectedTrajectorie.Track.AverageSpeed;

            txtSpeed.Text = speed.ToString("0.00") + "m/s    " + (speed * 3.6).ToString("0.00") + "km/h";
            txtDistance.Text = (arcMap.SelectedTrajectorie.Track.Distance / 1000).ToString("0.00") + "km";

            var movingSpeed = arcMap.SelectedTrajectorie.Track.GetMovingAverageSpeed();
            txtMovingSpeed.Text = movingSpeed.ToString("0.00") + "m/s    " + (movingSpeed * 3.6).ToString("0.00") + "km/h";
            txtMovingTime.Text = arcMap.SelectedTrajectorie.Track.GetMovingTime().ToString();
            txtMovingTime.Text = arcMap.SelectedTrajectorie.Track.GetMovingTime().ToString();

            var maxSpeed = arcMap.SelectedTrajectorie.Track.GetMaxSpeed();
            txtMaxSpeed.Text = maxSpeed.ToString("0.00") + "m/s    " + (maxSpeed * 3.6).ToString("0.00") + "km/h";
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(TrajectoriesFilePath))
            {
                string[] files = File.ReadAllLines(TrajectoriesFilePath);
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
            if (e.Trajectories.Length > 0)
            {
                lvwFiles.SelectedItem = e.Trajectories[e.Trajectories.Length - 1];
            }
        }

        private GpxInfo gpx;

        public GpxInfo Gpx
        {
            get => gpx;
            set => SetValueAndNotify(ref gpx, value, nameof(Gpx));
        }
        private GpxTrackInfo gpxTrack;

        public GpxTrackInfo GpxTrack
        {
            get => gpxTrack;
            set
            {
                SetValueAndNotify(ref gpxTrack, value, nameof(GpxTrack));
                grdLeft.IsEnabled = value != null;
            }
        }

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
        private void PointsGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pointsGrid.SelectedItem is GeoPoint point && point.Time.HasValue && point.Altitude != 0 && point.Latitude != 0)
            {

                chartHelper.SetLine(point.Time.Value);
                arcMap.SelectPointTo(point);
            }
            else
            {
                arcMap.ClearSelection();
                chartHelper.ClearLine();
            }
        }


        private void SpeedMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请选择单边采样率：", out string result, null, "3", "[1-9]", false, this))
            {
                if (int.TryParse(result, out int intResult))
                {
                    double speed = GpxTrack.Points.GetSpeed(pointsGrid.SelectedItem as GeoPoint, intResult);
                    MessageBox.ShowPrompt("速度为：" + speed.ToString("0.00") + "m/s，" + (3.6 * speed).ToString("0.00") + "km/h");
                }
            }
        }

        private void SpeedButtonClick(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请选择单边采样率：", out string result, null, "3", "[1-9]", false, this))
            {
                if (int.TryParse(result, out int intResult))
                {
                    foreach (var item in GpxTrack.Points)
                    {
                        double speed = GpxTrack.Points.GetSpeed(item as GeoPoint, intResult);
                        item.Speed = speed;
                    }
                }
            }

            var source = pointsGrid.ItemsSource;
            pointsGrid.ItemsSource = null;
            pointsGrid.ItemsSource = source;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string[] files =  FileSystemDialog.GetOpenFiles(new (string, string)[] { ("GPX轨迹文件", "gpx") }, true,true);
            if(files != null)
            {
                arcMap.LoadFiles(files);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请输入偏移值：", out string result, null, "", @"^[0-9]{0,5}(\.[0-9]+)?$", false, this))
            {
                if (double.TryParse(result, out double num))
                {
                    foreach (var point in GpxTrack.Points)
                    {
                        if (point.Altitude.HasValue)
                        {
                            point.Altitude += num;
                        }
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

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            arcMap.GraphicsOverlays.Clear();
            TrajectoryInfo.Trajectories.Clear();
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
