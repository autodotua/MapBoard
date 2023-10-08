using Android.Content;
using Android.OS;
using MapBoard.Platforms.Android;
using MapBoard.Services;

namespace MapBoard
{
    public class AndroidServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public bool ResumingTrack { get; set; }
        public TrackService TrackService { get; private set; }
        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            AndroidTrackServiceBinder binder = (AndroidTrackServiceBinder)service;
            var trackService = binder.Service;
            if (trackService.TrackService != null)
            {
                TrackService = trackService.TrackService;
            }
            else
            {
                TrackService = new TrackService();
                trackService.SetTrackServiceAndStart(TrackService);
            }
            if (ResumingTrack)
            {

            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
        }
    }
}
