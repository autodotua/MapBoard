using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using MapBoard.Model;
using MapBoard.Services;
using Debug = System.Diagnostics.Debug;
using Location = Android.Locations.Location;

namespace MapBoard.Platforms.Android;

public class TrackServiceAndroidImpl : TrackService
{
    private readonly AndroidLocationListener androidListener;
    private readonly GnssStatusCallback gnssStatusCallback;
    private readonly LocationManager manager;
    public TrackServiceAndroidImpl(Context context)
    {
        manager = context.GetSystemService(Context.LocationService) as LocationManager;
        gnssStatusCallback = new GnssStatusCallback(this);
        androidListener = new AndroidLocationListener(this);
    }
    protected override void BeginListening()
    {
        manager.RequestLocationUpdates(LocationManager.GpsProvider,
            Config.Instance.TrackMinTimeSpan * 1000,
            Config.Instance.TrackMinDistance,
            androidListener);
        manager.RegisterGnssStatusCallback(gnssStatusCallback, null);
    }

    protected override void EndListening()
    {
        manager.RemoveUpdates(androidListener);
        manager.UnregisterGnssStatusCallback(gnssStatusCallback);
    }
}

public class AndroidLocationListener(TrackService trackService) : Java.Lang.Object, ILocationListener
{
    public TrackService TrackService { get; } = trackService;

    public async void OnLocationChanged(Location location)
    {
        Debug.WriteLine("Android Location Listener Changed");
        Debug.Assert(TrackService != null);

        await TrackService.UpdateLocationAsync(new Microsoft.Maui.Devices.Sensors.Location()
        {
            Longitude = location.Longitude,
            Latitude = location.Latitude,
            Altitude = location.HasAltitude ? location.Altitude : null,
            Accuracy = location.HasAccuracy ? location.Accuracy : null,
            VerticalAccuracy = location.HasVerticalAccuracy ? location.VerticalAccuracyMeters : null,
            Course = location.HasBearing ? location.Bearing : null,
            Speed = location.HasSpeed ? location.Speed : null,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
        });

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

public class GnssStatusCallback : GnssStatus.Callback
{
    public GnssStatusCallback(TrackService trackService)
    {
        TrackService = trackService;
    }

    public TrackService TrackService { get; }

    public override void OnSatelliteStatusChanged(GnssStatus status)
    {
        base.OnSatelliteStatusChanged(status);
        TrackService.GnssStatus = new GnssStatusInfo()
        {
            Total = status.SatelliteCount,
            Fixed = Enumerable
                .Range(0, status.SatelliteCount)
                .Where(status.UsedInFix)
                .Count()
        };
    }
}
