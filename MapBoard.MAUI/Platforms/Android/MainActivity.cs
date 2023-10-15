﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using MapBoard.Platforms.Android;

namespace MapBoard
{
    [Activity(Theme = "@style/MyAppTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private bool resumingTrack = false;
        AndroidServiceConnection trackServiceConnection = new AndroidServiceConnection();
        public MainActivity()
        {
            Current = this;
        }

        public static MainActivity Current { get; private set; }
        public void BindTrackService()
        {
            if (IsServiceRunning(nameof(AndroidTrackService)))
            {
                trackServiceConnection.ResumingTrack = true;
                BindService(new Intent(this, typeof(AndroidTrackService)), trackServiceConnection, 0);
            }
        }

        public double GetNavBarHeight()
        {
            int resourceId = Resources.GetIdentifier("navigation_bar_height", "dimen", "android");
            if (resourceId > 0)
            {
                return Resources.GetDimensionPixelSize(resourceId);
            }
            return 0;
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

        public void StopTrackService()
        {
            UnbindService(trackServiceConnection);
            StopService(new Intent(this, typeof(AndroidTrackService)));
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
    }
}
