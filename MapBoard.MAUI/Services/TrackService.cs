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
        private GraphicsOverlay Overlay => MainMapView.Current.TrackOverlay;
        private readonly PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        private Gpx gpx = new Gpx();
        private GpxTrack gpxTrack;
        private DateTime startTime;
        private bool running = false;
        private Location lastLocation;
        public const double MinDistance = 2;
        public TrackService()
        {
            Overlay.Graphics.Clear();
            gpxTrack = gpx.CreateTrack();
        }

        public async void Start()
        {
            startTime = DateTime.Now;
            gpx.Time = startTime;
            gpx.Name = DateTime.Today.ToString("yyyyMMdd") + "轨迹";
            gpx.Description = AppInfo.Name;
            running = true;
            Overlay.Graphics.Clear();
            PolylineBuilder trackLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);
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
                UpdateMap(trackLineBuilder);
                if (lastLocation != null && Location.CalculateDistance(location, lastLocation, DistanceUnits.Kilometers) > MinDistance * 1000)
                {
                    UpdateGpx(location);
                    lastLocation = location;
                }
            }

        }

        public void Stop()
        {
            running = false;
            SaveGpx();
        }

        private void UpdateMap(PolylineBuilder builder)
        {
            Overlay.Graphics.Clear();
            Overlay.Graphics.Add(new Graphic(builder.ToGeometry()));
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

        private void SaveGpx()
        {
            File.WriteAllText(Path.Combine(FolderPaths.TrackPath, $"{startTime:yyyyMMdd-HHmmss}.gpx"), gpx.ToGpxXml());
        }

        public event EventHandler<GeolocationLocationChangedEventArgs> LocationChanged;
    }
}
