using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using MapBoard.Platforms.Android;
using MapBoard.Services;

namespace MapBoard
{
    [Activity(Theme = "@style/MyAppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        AndroidServiceConnection trackServiceConnection = new AndroidServiceConnection();
        private bool resumingTrack = false;
        public MainActivity()
        {
        }
        public void StopTrackService()
        {
            UnbindService(trackServiceConnection);
            StopService(new Intent(this, typeof(AndroidTrackService)));
        }
        public void StartTrackService()
        {
            if (IsServiceRunning(nameof(AndroidTrackService)))
            {
                throw new Exception("服务已经在运行");
            }
            Intent intent = new Intent(this, typeof(AndroidTrackService));
            StartForegroundService(intent);
            BindService(new Intent(this, typeof(AndroidTrackService)), trackServiceConnection, 0);
        }
        public void BindTrackService()
        {
            if (IsServiceRunning(nameof(AndroidTrackService)))
            {
                trackServiceConnection.ResumingTrack = true;
                BindService(new Intent(this, typeof(AndroidTrackService)), trackServiceConnection, 0);
            }
        }

        private bool IsServiceRunning(string name)
        {
            ActivityManager manager = GetSystemService(ActivityService) as ActivityManager;
            foreach (var service in manager.GetRunningServices(10))
            {
                if (service.Service.ClassName == name)
                {
                    return true;
                }
            }
            return false;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (IsServiceRunning(nameof(AndroidTrackService)))
            {
                UnbindService(trackServiceConnection);
            }
        }
    }


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
