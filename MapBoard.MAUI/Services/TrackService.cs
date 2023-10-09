using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using FzLib;
using MapBoard.IO;
using MapBoard.IO.Gpx;
using MapBoard.Mapping;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XmpCore.Impl;

namespace MapBoard.Services
{
    public class TrackService : INotifyPropertyChanged
    {
        public const double MinDistance = 2;
        private static TrackService current;
        private readonly PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        private GnssStatusInfo gnssStatus;
        private Gpx gpx = new Gpx();
        private GpxTrack gpxTrack;
        private Location lastLocation;
        /// <summary>
        /// 暂停标志。若置改标志为true然后Stop，那么下一次启动时，会继续本次轨迹。
        /// </summary>
        private bool pausingFlag = false;

        private int pointsCount;
        private bool running = false;
        private DateTime startTime = DateTime.Now;
        private double tolerableAccuracy = 20;
        private double totalDistance;
        private DateTime updateTime = DateTime.Now;

        public TrackService()
        {
            Overlay.Graphics.Clear();
        }

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        public event EventHandler<GeolocationLocationChangedEventArgs> LocationChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 当前实例
        /// </summary>
        public static TrackService Current
        {
            get => current;
            set
            {
                current = value;
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(Current)));
            }
        }

        /// <summary>
        /// 设置恢复记录的轨迹文件，当调用Start方法时，若该值非null，则会读取该文件然后继续记录
        /// </summary>
        public static string ResumeGpx { get; set; } = null;

        /// <summary>
        /// GNSS状态信息
        /// </summary>
        public GnssStatusInfo GnssStatus
        {
            get => gnssStatus;
            private set => this.SetValueAndNotify(ref gnssStatus, value, nameof(GnssStatus));
        }

        /// <summary>
        /// 最后一个采集的位置
        /// </summary>
        public Location LastLocation
        {
            get => lastLocation;
            private set => this.SetValueAndNotify(ref lastLocation, value, nameof(LastLocation));
        }

        /// <summary>
        /// 已经记录的点数量
        /// </summary>
        public int PointsCount
        {
            get => pointsCount;
            set => this.SetValueAndNotify(ref pointsCount, value, nameof(PointsCount));
        }

        /// <summary>
        /// 开始记录的时间
        /// </summary>
        public DateTime StartTime
        {
            get => startTime;
            private set => this.SetValueAndNotify(ref startTime, value, nameof(StartTime));
        }

        /// <summary>
        /// 总里程
        /// </summary>
        public double TotalDistance
        {
            get => totalDistance;
            private set => this.SetValueAndNotify(ref totalDistance, value, nameof(TotalDistance));
        }

        /// <summary>
        /// 位置更新时间
        /// </summary>
        public DateTime UpdateTime
        {
            get => updateTime;
            private set => this.SetValueAndNotify(ref updateTime, value, nameof(UpdateTime));
        }

        private GraphicsOverlay Overlay => MainMapView.Current.TrackOverlay;

        public void PutPausingFlag()
        {
            pausingFlag = true;
        }
        public async void Start()
        {
            if (running)
            {
                throw new Exception("单个实例不可多次开始记录轨迹");
            }
            running = true;
            Current = this;

            Overlay.Graphics.Clear();
            PolylineBuilder trackLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);
            double distance = 0;
            //不知道是否会出现仅进行GetLocationAsync时才调用GPS，所以用这个方法抓住GPS
            await Geolocation.Default.StartListeningForegroundAsync(new GeolocationListeningRequest(GeolocationAccuracy.Best)).ConfigureAwait(false);

            if (ResumeGpx != null)
            {
                gpx = await Gpx.FromFileAsync(ResumeGpx);
                ResumeGpx = null;
                gpxTrack = gpx.Tracks[0];
                StartTime = gpx.Time;
                PointsCount = gpxTrack.Points.Count;
                foreach (var point in gpxTrack.Points)
                {
                    trackLineBuilder.AddPoint(new MapPoint(point.X, point.Y));
                }
                UpdateMap(trackLineBuilder);
            }
            else
            {
                gpxTrack = gpx.CreateTrack();
                gpx.Time = StartTime = DateTime.Now;
                gpx.Name = DateTime.Today.ToString("yyyyMMdd") + "轨迹";
                gpx.Creator = AppInfo.Name;
            }

            while (running)
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
                if (running == false)
                {
                    break;
                }
                if (location == null)
                {
                    throw new Exception("无法获取位置");
                }
                if (location.Accuracy > tolerableAccuracy || location.Altitude == null) //通常是基站和WIFI定位结果，排除
                {
                    continue;
                }
                LocationChanged?.Invoke(this, new GeolocationLocationChangedEventArgs(location));
                trackLineBuilder.AddPoint(location.Longitude, location.Latitude);

                if (LastLocation == null)
                {
                    LastLocation = location;
                }
                distance = Location.CalculateDistance(location, LastLocation, DistanceUnits.Kilometers) * 1000;
                if (PointsCount == 0 || distance > MinDistance)
                {
                    TotalDistance += distance;
                    UpdateMap(trackLineBuilder);
                    UpdateGpx(location);
                    LastLocation = location;
                    PointsCount++;
                }
                UpdateTime = location.Timestamp.LocalDateTime;
                await timer.WaitForNextTickAsync();
            }

        }


        public void Stop()
        {
            running = false;
            Current = null;
            if (pausingFlag) //如果时暂停，那么保存当前GPX文件路径，供下次启动时读取
            {
                if (File.Exists(GetGpxFilePath()))
                {
                    ResumeGpx = GetGpxFilePath();
                }
            }
            Geolocation.Default.StopListeningForeground();
            SaveGpx();
        }


        public void UpdateGnssStatus(GnssStatusInfo gpsStatus)
        {
            GnssStatus = gpsStatus;
        }

        private string GetGpxFilePath()
        {
            return Path.Combine(FolderPaths.TrackPath, $"{StartTime:yyyyMMdd-HHmmss}.gpx");
        }

        private void SaveGpx()
        {
            if (gpxTrack.Points.Count >= 3)
            {
                File.WriteAllText(GetGpxFilePath(), gpx.ToGpxXml());
            }
        }
        private void UpdateGpx(Location location)
        {
            try
            {
                GpxPoint point = new GpxPoint(location.Longitude, location.Latitude, location.Altitude, location.Timestamp.LocalDateTime);
                if (location.Accuracy.HasValue)
                {
                    point.OtherProperties.Add("Accuracy", location.Accuracy.ToString());
                }
                if (location.VerticalAccuracy.HasValue)
                {
                    point.OtherProperties.Add("VerticalAccuracy", location.VerticalAccuracy.ToString());
                }
                if (location.Course.HasValue)
                {
                    point.OtherProperties.Add("Course", location.Course.ToString());
                }
                if (location.Speed.HasValue)
                {
                    point.OtherProperties.Add("Speed", location.Speed.ToString());
                }
                gpxTrack.Points.Add(point);

                if (gpxTrack.Points.Count % 10 == 0)
                {
                    SaveGpx();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void UpdateMap(PolylineBuilder builder)
        {
            Overlay.Graphics.Clear();
            Overlay.Graphics.Add(new Graphic(builder.ToGeometry()));
        }
    }
}
