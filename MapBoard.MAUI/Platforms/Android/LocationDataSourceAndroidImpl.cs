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

namespace MapBoard.Platforms.Android
{
    public class LocationDataSourceAndroidImpl : LocationDataSource
    {
        private AndroidDisplayLocationListener locationListener;
        private AndroidDisplaySensorListener sensorListener;
        private LocationManager locationManager;
        private SensorManager sensorManager;
        public LocationDataSourceAndroidImpl()
        {
            locationListener = new AndroidDisplayLocationListener(UpdateLocation);
            sensorListener = new AndroidDisplaySensorListener(UpdateHeading);
            locationManager = Platform.AppContext.GetSystemService(Context.LocationService) as LocationManager;
            sensorManager = Platform.AppContext.GetSystemService(Context.SensorService) as SensorManager;

        }

        protected override async Task OnStartAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    locationManager.RequestLocationUpdates(LocationManager.GpsProvider,
                        Config.Instance.TrackMinTimeSpan * 1000,
                        Config.Instance.TrackMinDistance,
                        locationListener);
                    sensorManager.RegisterListener(sensorListener,
                        sensorManager.GetDefaultSensor(SensorType.Accelerometer),
                        SensorDelay.Ui);
                    sensorManager.RegisterListener(sensorListener,
                        sensorManager.GetDefaultSensor(SensorType.MagneticField),
                        SensorDelay.Ui);
                });

            }
            catch (Exception ex)
            {
                throw;
            }

        }

        protected async override Task OnStopAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    locationManager.RemoveUpdates(locationListener);
                    sensorManager.UnregisterListener(sensorListener);
                });
            }
            catch (Exception ex)
            {
                throw;
            };
        }

        internal void CallLocationChanged(Esri.ArcGISRuntime.Location.Location location)
        {
            UpdateLocation(location);
        }
    }

    public class AndroidDisplayLocationListener(Action<Esri.ArcGISRuntime.Location.Location> updateLocation) : Java.Lang.Object, ILocationListener
    {
        public void OnLocationChanged(global::Android.Locations.Location location)
        {
            MapPoint point;

            point = new MapPoint(location.Longitude, location.Latitude, location.HasAltitude ? location.Altitude : 0, SpatialReferences.Wgs84);
            updateLocation(new Esri.ArcGISRuntime.Location.Location(
               point,
               location.HasAccuracy ? location.Accuracy : double.NaN,
               location.HasSpeed ? location.Speed : 0,
               location.HasBearing ? location.Bearing : 0,
               false
            ));
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

    public class AndroidDisplaySensorListener(Action<double> updateHeading) : Java.Lang.Object, ISensorEventListener
    {
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
                    updateHeading(azimuth);
                }
            }
        }
    }
}
