using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using MapBoard.Platforms.Android;

namespace MapBoard
{
    [Activity(Theme = "@style/MyAppTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
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
                if (service.Service.ClassName.EndsWith(name))
                {
                    return true;
                }
            }
            return false;
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (IsServiceRunning(nameof(AndroidTrackService)))
            {
                BindService(new Intent(this, typeof(AndroidTrackService)), trackServiceConnection, 0);
            }
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
}
