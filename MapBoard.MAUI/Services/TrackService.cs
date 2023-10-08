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
        public TrackService() { }
        private GraphicsOverlay Overlay => MainMapView.Current.TrackOverlay;
        public void Initialize()
        {
            Geolocation.Default.LocationChanged += Default_LocationChanged;
            Overlay.Graphics.Clear();
        }

        public void Start()
        {
            GeolocationListeningRequest request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
            Geolocation.Default.StartListeningForegroundAsync(request);
        }

        public void Stop()
        {
            Geolocation.Default.StopListeningForeground();
        }

        public List<MapPoint> trackPoints = new List<MapPoint>();
        public List<Location> trackLocations = new List<Location>();
        PolylineBuilder trackLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);
        private void Default_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
        {
            Debug.WriteLine(e.Location);
            trackLocations.Add(e.Location);
            trackLineBuilder.AddPoint(e.Location.Longitude, e.Location.Latitude);
            UpdateMap();
            UpdateGpx();
        }

        private void UpdateMap()
        {
            Overlay.Graphics.Clear();
            Overlay.Graphics.Add(new Graphic(trackLineBuilder.ToGeometry()));
        }
        private void UpdateGpx()
        {

        }


        public event EventHandler LocationChanged;
    }
}
