using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
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
        private PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        private bool running = false;
        public TrackService()
        {
            Overlay.Graphics.Clear();
        }

        public async void Start()
        {
            running = true;
            Overlay.Graphics.Clear();
            while (running)
            {
                var location =await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));
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
                trackLocations.Add(location);
                trackLineBuilder.AddPoint(location.Longitude, location.Latitude);
                UpdateMap();
                UpdateGpx();
            }

        }

        public void Stop()
        {
            running = false;
        }

        public List<MapPoint> trackPoints = new List<MapPoint>();
        public List<Location> trackLocations = new List<Location>();
        PolylineBuilder trackLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);


        private void UpdateMap()
        {
            Overlay.Graphics.Clear();
            Overlay.Graphics.Add(new Graphic(trackLineBuilder.ToGeometry()));
        }
        private void UpdateGpx()
        {

        }


        public event EventHandler<GeolocationLocationChangedEventArgs> LocationChanged;
    }
}
