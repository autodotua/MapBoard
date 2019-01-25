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
        TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint> timeChartHelper;
        public MainWindow()
        {
            InitializeComponent();
            timeChartHelper = new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint>(speedChart);

            timeChartHelper.XAxisLineValueConverter = p => p.CenterTime;
            timeChartHelper.XAxisPointValueConverter = p => p.CenterTime;
            timeChartHelper.XAxisPolygonValueConverter = p => p.Time.Value;

            timeChartHelper.YAxisPointValueConverter = p => p.Speed;
            timeChartHelper.YAxisLineValueConverter = p => p.Speed;
            timeChartHelper.YAxisPolygonValueConverter = p => p.Altitude ?? double.NaN;
            timeChartHelper.XLabelFormat = p => p.ToString("HH:mm");
            timeChartHelper.YLabelFormat = p => p + "m/s";
            timeChartHelper.ToolTipConverter = p =>
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

            timeChartHelper.MouseOverPoint += (p1, p2) =>
              {
                  //arcMap.ClearSelection();
                  arcMap.SelectPointTo(p2.Item.RelatedPoints[0]);
              };
            timeChartHelper.LinePointEnbale = p => p.Speed > 0.2;

            //TaskDialog.DefaultOwner = this;
        }



        private  void StartOffset(bool north, double value)
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

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            double value = Keyboard.Modifiers == ModifierKeys.Control ? 1 : 10;
            switch ((sender as Button).Content as string)
            {
                case "↑":
                    StartOffset(true, value);
                    break;
                case "↓":
                    StartOffset(true, -value);
                    break;
                case "←":
                    StartOffset(false, -value);
                    break;
                case "→":
                    StartOffset(false, +value);
                    break;
            }
        }

        private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // stop = true;
            //map.LoadGpxs();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            File.WriteAllLines(TrajectoriesFilePath, TrajectoryInfo.Trajectories.Select(p => p.FilePath));
            // map.Dispose();
        }

        private void ListView_ItemPreviewDeleteKeyDown(object sender, KeyEventArgs e)
        {
            foreach (var item in lvwFiles.SelectedItems.Cast<TrajectoryInfo>().ToArray())
            {
                arcMap.GraphicsOverlays.Remove(item.Overlay);
                TrajectoryInfo.Trajectories.Remove(item);

            }
        }
        private void lvwFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            //foreach (var g in item.Overlay.Graphics.Where(p => p.IsSelected))
            //{
            //    g.IsSelected = false;
            //}

            arcMap.SelectedTrajectorie = lvwFiles.SelectedItem as TrajectoryInfo;

            arcMap.SelectedTrajectorie.Overlay.Renderer = GetCurrentOverlayRenderer();



            arcMap.SetViewpointGeometryAsync(arcMap.SelectedTrajectorie.Overlay.Extent);
            var points = GetUsableSpeeds(arcMap.SelectedTrajectorie.Track.Points);
            var lines = GetFilteredSpeeds(arcMap.SelectedTrajectorie.Track.Points, 20, 5);
            timeChartHelper.DrawAction = () =>
              {
                  timeChartHelper.Initialize();
                  timeChartHelper.DrawBorder(points, true, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint>.BorderSetting<SpeedInfo>()
                  {
                      XAxisBorderValueConverter = p => p.CenterTime,
                      YAxisBorderValueConverter = p => p.Speed,
                  });
                  timeChartHelper.DrawBorder(arcMap.SelectedTrajectorie.Track.Points.TimeOrderedPoints.Where(p => p.Altitude.HasValue), false, new TimeBasedChartHelper<SpeedInfo, SpeedInfo, GeoPoint>.BorderSetting<GeoPoint>()
                  {
                      XAxisBorderValueConverter = p => p.Time.Value,
                      YAxisBorderValueConverter = p => p.Altitude.Value,
                  });
                  timeChartHelper.DrawPolygon(arcMap.SelectedTrajectorie.Track.Points, 1);
                  timeChartHelper.DrawPoints(points, 0);
                  timeChartHelper.DrawLines(lines, 0);
              };
            timeChartHelper.DrawAction();

            GpxInfoModel = arcMap.SelectedTrajectorie.Gpx;
            GpxTrackInfoModel = arcMap.SelectedTrajectorie.Track;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(TrajectoriesFilePath))
            {
                string[] files = File.ReadAllLines(TrajectoriesFilePath);
                arcMap.LoadFiles(files);
            }
        }

        private void speedChart_MouseLeave(object sender, MouseEventArgs e)
        {
            arcMap.ClearSelection();
        }

        private void arcMap_PointSelected(object sender, ArcMapView.PointSelectedEventArgs e)
        {
            if (Cursor == Cursors.Help)
            {
                pointsGrid.SelectedItem = e.Point;
                pointsGrid.ScrollIntoView(e.Point);
                Cursor = Cursors.Arrow;
            }
        }

        private void arcMap_GpxLoaded(object sender, ArcMapView.GpxLoadedEventArgs e)
        {
            if (e.Trajectories.Length > 0)
            {
                lvwFiles.SelectedItem = e.Trajectories[e.Trajectories.Length - 1];
            }
        }
        private GpxInfo gpxInfoModel;

        public GpxInfo GpxInfoModel
        {
            get => gpxInfoModel;
            set => SetValueAndNotify(ref gpxInfoModel, value, nameof(GpxInfoModel));
        }
        private GpxTrackInfo gpxTrackInfoModel;

        public GpxTrackInfo GpxTrackInfoModel
        {
            get => gpxTrackInfoModel;
            set => SetValueAndNotify(ref gpxTrackInfoModel, value, nameof(GpxTrackInfoModel));
        }

        private void AzureDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void AzureDataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            AzureDataGrid_LoadingRow(sender, e);
            if (pointsGrid.Items != null)
            {
                for (int i = 0; i < pointsGrid.Items.Count; i++)
                {
                    try
                    {
                        if (pointsGrid.ItemContainerGenerator.ContainerFromIndex(i) is DataGridRow row)
                        {
                            row.Header = (i + 1).ToString();
                        }
                    }
                    catch { }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursor == Cursors.Help ? Cursors.Arrow : Cursors.Help;
        }

        private void pointsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pointsGrid.SelectedItem != null)
            {
                arcMap.SelectPointTo(pointsGrid.SelectedItem as GeoPoint);
            }
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请选择单边采样率：", out string result, null, "3", "[1-9]", false, this))
            {
                if (int.TryParse(result, out int intResult))
                {
                    double speed = GpxTrackInfoModel.Points.GetSpeed(pointsGrid.SelectedItem as GeoPoint, intResult);
                    MessageBox.ShowPrompt("速度为：" + speed.ToString("0.00") + "m/s，" + (3.6 * speed).ToString("0.00") + "km/h");
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (InputBox.GetInput("请选择单边采样率：", out string result, null, "3", "[1-9]", false, this))
            {
                if (int.TryParse(result, out int intResult))
                {
                    foreach (var item in GpxTrackInfoModel.Points)
                    {
                        double speed = GpxTrackInfoModel.Points.GetSpeed(item as GeoPoint, intResult);
                        item.Speed = speed;
                    }
                }
            }

            var source = pointsGrid.ItemsSource;
            pointsGrid.ItemsSource = null;
            pointsGrid.ItemsSource = source;
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
