using Android.OS;

namespace MapBoard.Platforms.Android;

public class AndroidTrackServiceBinder : Binder
{
    public AndroidTrackServiceBinder(AndroidTrackService service)
    {
        Service = service;
    }

    public AndroidTrackService Service { get; }
}

