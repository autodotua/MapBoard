using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.IO;
using MapBoard.IO.Gpx;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Services
{
    public class TrackService
    {
        public const double MinDistance = 2;
        private readonly PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        private Gpx gpx = new Gpx();
        private GpxTrack gpxTrack;
        private Location lastLocation;
        private bool running = false;
        public TrackService()
        {
            Overlay.Graphics.Clear();
            gpxTrack = gpx.CreateTrack();
        }

        public event EventHandler<GeolocationLocationChangedEventArgs> LocationChanged;

        public DateTime StartTime { get; private set; }
        private GraphicsOverlay Overlay => MainMapView.Current.TrackOverlay;

        public double TotalDistance { get; private set; }

        public async void Start()
        {
            StartTime = DateTime.Now;
            gpx.Time = StartTime;
            gpx.Name = DateTime.Today.ToString("yyyyMMdd") + "轨迹";
            gpx.Creator = AppInfo.Name;
            running = true;
            Overlay.Graphics.Clear();
            PolylineBuilder trackLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);
            double distance = 0;
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
                Debug.WriteLine(location);
                LocationChanged?.Invoke(this, new GeolocationLocationChangedEventArgs(location));
                trackLineBuilder.AddPoint(location.Longitude, location.Latitude);

                if (lastLocation == null)
                {
                    lastLocation = location;
                }
                else
                {
                    distance = Location.CalculateDistance(location, lastLocation, DistanceUnits.Kilometers) * 1000;
                    if (distance > MinDistance)
                    {
                        TotalDistance += distance;
                        UpdateMap(trackLineBuilder);
                        UpdateGpx(location);
                        lastLocation = location;
                    }
                }
                await timer.WaitForNextTickAsync();
            }

        }

        public void Stop()
        {
            running = false;
            SaveGpx();
        }

        private void SaveGpx()
        {
            if (gpxTrack.Points.Count >= 3)
            {
                File.WriteAllText(Path.Combine(FolderPaths.TrackPath, $"{StartTime:yyyyMMdd-HHmmss}.gpx"), gpx.ToGpxXml());
            }
        }

        private void UpdateGpx(Location location)
        {
            try
            {
                GpxPoint point = new GpxPoint(location.Longitude, location.Latitude, location.Altitude ?? 0, location.Timestamp.LocalDateTime);
                point.OtherProperties.Add("Accuracy", location.Accuracy.ToString());
                point.OtherProperties.Add("VerticalAccuracy", location.VerticalAccuracy?.ToString() ?? "");
                point.OtherProperties.Add("Course", location.Course?.ToString() ?? "");
                gpxTrack.Points.Add(point);
                Debug.WriteLine("add to gpx");
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
