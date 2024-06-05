using Android.Content;
using Android.Hardware;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using MapBoard.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;
using Trace = System.Diagnostics.Trace;

namespace MapBoard.Platforms.Android
{
    public class LocationDataSourceAndroidImpl : LocationDataSource
    {
        private LocationDisplayLocationListener locationListener;
        private LocationDisplaySensorListener sensorListener;
        private LocationDisplayGnssListener gnssListener;

        private LocationManager locationManager;
        private SensorManager sensorManager;

        private bool isListening = false;
        public LocationDataSourceAndroidImpl()
        {
            Debug.WriteLine("Create new LocationDataSourceAndroidImpl");
            locationListener = new LocationDisplayLocationListener();
            locationListener.LocationChanged += LocationListener_LocationChanged;

            sensorListener = new LocationDisplaySensorListener();
            sensorListener.AzimuthCanged += SensorListener_AzimuthCanged;

            gnssListener = new LocationDisplayGnssListener();
            gnssListener.IsFixedChanged += GnssListener_IsFixedChanged;

            locationManager = Platform.AppContext.GetSystemService(Context.LocationService) as LocationManager;
            sensorManager = Platform.AppContext.GetSystemService(Context.SensorService) as SensorManager;

        }

        private void GnssListener_IsFixedChanged(object sender, LocationDisplayGnssListener.IsGnssFixedChangedEventArgs e)
        {
        }

        private void SensorListener_AzimuthCanged(object sender, LocationDisplaySensorListener.AzimuthCangedEventArgs e)
        {
            UpdateHeading(e.Azimuth);
        }

        private void LocationListener_LocationChanged(object sender, LocationChangedEventArgs e)
        {
            var location = e.Location;
            Debug.WriteLine($"Provider: {location.Provider}; Is fixed: {gnssListener.IsFixed}");
            switch (location.Provider)
            {
                case LocationManager.GpsProvider when gnssListener.IsFixed:
                    UpdateLocation(location);
                    break;
                case LocationManager.FusedProvider when !gnssListener.IsFixed:
                    UpdateLocation(location);
                    break;
            }
        }

        private void UpdateLocation(global::Android.Locations.Location location)
        {
            UpdateLocation(new Esri.ArcGISRuntime.Location.Location(
                location.HasAltitude ?
                    new MapPoint(location.Longitude, location.Latitude, location.Altitude, SpatialReferences.Wgs84)
                    : new MapPoint(location.Longitude, location.Latitude, SpatialReferences.Wgs84),
                location.HasAccuracy ? location.Accuracy : double.NaN,
                location.HasSpeed ? location.Speed : 0,
                location.HasBearing ? location.Bearing : double.NaN,
                false
                ));
        }

        protected override async Task OnStartAsync()
        {
            if (isListening)
            {
                Debug.WriteLine("DataSource abort startting");
                return;
            }
            Debug.WriteLine("DataSource startting");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                locationManager.RequestLocationUpdates(LocationManager.GpsProvider,
                    Config.Instance.TrackMinTimeSpan * 1000,
                    Config.Instance.TrackMinDistance,
                    locationListener);
                locationManager.RequestLocationUpdates(LocationManager.FusedProvider,
                    Config.Instance.TrackMinTimeSpan * 1000,
                    Config.Instance.TrackMinDistance,
                    locationListener);
                locationManager.RegisterGnssStatusCallback(gnssListener, null);
                sensorManager.RegisterListener(sensorListener,
                    sensorManager.GetDefaultSensor(SensorType.Accelerometer),
                    SensorDelay.Ui);
                sensorManager.RegisterListener(sensorListener,
                    sensorManager.GetDefaultSensor(SensorType.MagneticField),
                    SensorDelay.Ui);
                isListening = true;
            });
            Debug.WriteLine("DataSource started");
        }

        protected override Task OnStopAsync()
        {
            Debug.WriteLine("DataSource stopping");
            isListening = false;
            locationManager.RemoveUpdates(locationListener);
            locationManager.RemoveUpdates(locationListener);
            locationManager.UnregisterGnssStatusCallback(gnssListener);
            sensorManager.UnregisterListener(sensorListener);
            Debug.WriteLine("DataSource stopped");
            return Task.CompletedTask;
        }
    }

    public class LocationDisplayGnssListener : GnssStatus.Callback
    {
        private bool isFixed;

        public bool IsFixed
        {
            get => isFixed;
            private set
            {
                if(isFixed != value)
                {
                    IsFixedChanged?.Invoke(this, new IsGnssFixedChangedEventArgs(value));
                }
                isFixed = value;
            }
        }

        public event EventHandler<IsGnssFixedChangedEventArgs> IsFixedChanged;

        public override void OnSatelliteStatusChanged(GnssStatus status)
        {
            base.OnSatelliteStatusChanged(status);
            Debug.WriteLine($"Satellite Status Changed: {Enumerable.Range(0, status.SatelliteCount).Where(status.UsedInFix).Count()}/{status.SatelliteCount}");

            int fixedSatellitesCount = 0;
            int satelliteCount = status.SatelliteCount;

            for (int i = 0; i < satelliteCount; i++)
            {
                if (status.UsedInFix(i))
                {
                    fixedSatellitesCount++;
                    if (fixedSatellitesCount >= 4)
                    {
                        IsFixed = true;
                        return;
                    }
                }
            }
            IsFixed = false;
        }

        public class IsGnssFixedChangedEventArgs(bool isFixed) : EventArgs
        {
            public bool IsFixed { get; } = isFixed;
        }
    }

    public class LocationDisplayLocationListener : Java.Lang.Object, ILocationListener
    {
        public event EventHandler<LocationChangedEventArgs> LocationChanged;
        public void OnLocationChanged(global::Android.Locations.Location location)
        {
            LocationChanged?.Invoke(this, new LocationChangedEventArgs(location));
        }

        public void OnProviderDisabled(string provider)
        {
            Debug.WriteLine("Android Location Listener Disabled");
        }

        public void OnProviderEnabled(string provider)
        {
            Debug.WriteLine("Android Location Listener Enabled");
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            Debug.WriteLine("Android Location Status Changed：" + status.ToString());
        }

    }

    public class LocationDisplaySensorListener : Java.Lang.Object, ISensorEventListener
    {
        public event EventHandler<AzimuthCangedEventArgs> AzimuthCanged;
        private float[] gravity;
        private float[] magnetic;

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (e.Sensor.Type == SensorType.Accelerometer)
            {
                gravity = [.. e.Values];
            }
            else if (e.Sensor.Type == SensorType.MagneticField)
            {
                magnetic = [.. e.Values];
            }
            if (gravity != null && magnetic != null)
            {
                float[] R = new float[9];
                float[] I = new float[9];
                bool success = SensorManager.GetRotationMatrix(R, I, gravity, magnetic);
                if (success)
                {
                    float[] orientation = new float[3];
                    SensorManager.GetOrientation(R, orientation);
                    var azimuth = (orientation[0] * (180 / Math.PI) + 360) % 360;
                    AzimuthCanged?.Invoke(this, new AzimuthCangedEventArgs(azimuth));
                }
            }
        }

        public class AzimuthCangedEventArgs(double azimuth) : EventArgs
        {
            public double Azimuth { get; } = azimuth;
        }
    }
}
