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
    /**
     * 对于没有各种限制的Windows来说，只要使用这个类，调用Start开始，Stop结束即可
     * 但是Android需要把这个类挂在Service下面才合理。
     * 用JAVA写的时候，挂在Activity下面一会就被杀了，或者关屏后不再记录，所以需要在Service下面处理。
     * 虽然不清楚MAUI如何，但也用了相同的方法。
     * 对于Android，要开始记录轨迹时，调用MainActivity下的StartTrackService()，
     * 来启动AndroidTrackService前台服务，并绑定服务。
     * 通过AndroidServiceConnection类来实现Service和Activity的交互。
     * 绑定成功后，AndroidServiceConnection下的OnServiceConnected方法会被调用，
     * 这个方法会从AndroidTrackServiceBinder对象中拿到AndroidTrackService服务对象，
     * 如果AndroidTrackService中的TrackService对象为空，说明是第一次绑定，那么就建立这个对象，
     * 然后调用TrackService.Start()来开始记录轨迹。
     * 如果不为空，那么说明时Activity被杀后重新和Servcice建立联系。
     * 需要停止记录时，调用MainActivity下的StopTrackService()来实现。
     * StopTrackService做了两件事，解绑和停止服务。
     * 在AndroidTrackService的OnDestroy()方法下，调用了TrackService.Stop()，实现轨迹记录的停止。
     * AndroidTrackService开始后，还会初始化AndroidGnssHelper类，
     * 这个类拥抱LocationManager来读取卫星详细信息。
     * 然后在卫星信息改变时，调用TrackService.UpdateGnssStatus进行更新。
     */

    public class TrackService : INotifyPropertyChanged
    {
        public const double MinDistance = 2;
        private static TrackService current;
        private readonly PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        private GnssStatusInfo gnssStatus;
        private Gpx gpx = new Gpx();
        private GpxTrack gpxTrack;
        private Location lastLocation;

        private int pointsCount;
        private bool running = false;
        private DateTime startTime = DateTime.Now;
        private double tolerableAccuracy = 20;
        private double totalDistance;
        private DateTime updateTime = DateTime.Now;

        public TrackService()
        {
            Overlay.Clear();
        }

        public static event ThreadExceptionEventHandler ExceptionThrown;

        public static event EventHandler CurrentChanged;
        public static event EventHandler<GpxSavedEventArgs> GpxSaved;

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
                if (current == null && value == null || current != null && value != null)
                {
                    throw new ArgumentException("设置Current时，若当前为null，必须提供值；若当前非null，必须设置为null");
                }
                current = value;
                CurrentChanged?.Invoke(null, EventArgs.Empty);
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

        public TimeSpan Duration => UpdateTime - StartTime;

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
            private set => this.SetValueAndNotify(ref updateTime, value, nameof(UpdateTime), nameof(Duration));
        }

        private TrackOverlayHelper Overlay => MainMapView.Current.TrackOverlay;
        public async void Start()
        {
            if (running)
            {
                throw new Exception("单个实例不可多次开始记录轨迹");
            }
            running = true;
            Current = this;

            try
            {
                Overlay.Clear();
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
                    //Overlay.LoadLine(gpxTrack.Points.Select(p => new MapPoint(p.X, p.Y)));
                    await Overlay.LoadColoredGpxAsync(gpx);
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
                    Location location = null;
#if WINDOWS
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                       {
#endif
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
#if WINDOWS
                });
#endif
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


                    if (LastLocation == null)
                    {
                        LastLocation = location;
                    }
                    distance = Location.CalculateDistance(location, LastLocation, DistanceUnits.Kilometers) * 1000;
                    if (PointsCount == 0 || distance > MinDistance)
                    {
                        TotalDistance += distance;
                        Overlay.AddPoint(location.Longitude, location.Latitude, location.Timestamp.LocalDateTime, location.Speed ?? 0d, location.Altitude);
                        UpdateGpx(location);
                        LastLocation = location;
                        PointsCount++;
                    }
                    UpdateTime = location.Timestamp.LocalDateTime;
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Stop();
                ExceptionThrown?.Invoke(this, new ThreadExceptionEventArgs(ex));
            }
        }


        public void Stop()
        {
            if (!running)
            {
                return;
            }
            running = false;
            Current = null;
            try
            {
                Geolocation.Default.StopListeningForeground();
                if (SaveGpx())
                {
                    GpxSaved?.Invoke(this, new GpxSavedEventArgs(GetGpxFilePath()));
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void UpdateGnssStatus(GnssStatusInfo gpsStatus)
        {
            GnssStatus = gpsStatus;
        }

        private string GetGpxFilePath()
        {
            return Path.Combine(FolderPaths.TrackPath, $"{StartTime:yyyyMMdd-HHmmss}.gpx");
        }

        private bool SaveGpx()
        {
            if (gpxTrack != null && gpxTrack.Points.Count >= 2)
            {
                File.WriteAllText(GetGpxFilePath(), gpx.ToGpxXml());
                return true;
            }
            return false;
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

        public class GpxSavedEventArgs : EventArgs
        {
            public GpxSavedEventArgs(string path)
            {
                FilePath = path;
            }
            public string FilePath { get; init; }
        }
    }
}
