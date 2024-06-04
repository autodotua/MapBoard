using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Widget;
using MapBoard.Mapping;
using MapBoard.Platforms.Android;
using MapBoard.Services;
using MapBoard.Views;

namespace MapBoard
{
    [Activity(Theme = "@style/MyAppTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private bool hasPressedBack = false;
        AndroidServiceConnection trackServiceConnection = new AndroidServiceConnection();

        public void BindTrackService()
        {
            if (AndroidTrackService.IsRunning)
            {
                trackServiceConnection.ResumingTrack = true;
                BindService(new Intent(this, typeof(AndroidTrackService)), trackServiceConnection, 0);
            }
        }

        public double GetNavBarHeight()
        {
            if (Build.VERSION.SdkInt >= (BuildVersionCodes)30)
            {
                return WindowManager.CurrentWindowMetrics.WindowInsets.GetInsets(WindowInsets.Type.NavigationBars()).Bottom;
            }
            var orientation = Resources.Configuration.Orientation;
            int resourceId = Resources.GetIdentifier(orientation == Android.Content.Res.Orientation.Portrait ? "navigation_bar_height" : "navigation_bar_height_landscape", "dimen", "android");
            if (resourceId > 0)
            {
                return Resources.GetDimensionPixelSize(resourceId);
            }
            return 0;
        }

        public override async void OnBackPressed()
        {
            if (MainMapView.Current.CurrentStatus == Models.MapViewStatus.Draw)
            {
                MainMapView.Current.Editor.Cancel();
            }
            else if (MainMapView.Current.CurrentStatus == Models.MapViewStatus.Select)
            {
                MainMapView.Current.ClearSelection();
            }
            else if (MainPage.Current.IsAnyNotStandalonePanelOpened())
            {
                MainPage.Current.CloseAllPanel();
            }
            else
            {
                if (hasPressedBack)
                {
                    OnBackPressedDispatcher.OnBackPressed();
                    if (AndroidTrackService.IsRunning)
                    {
                        Finish();
                    }
                    else
                    {
                        MapBoard.App.Current.Quit();
                    }
                    return;
                }

                hasPressedBack = true;
                Toast.MakeText(this, "再按一次退出应用", ToastLength.Short).Show();
                await Task.Delay(2000);
                hasPressedBack = false;
            }
        }

        public void StartTrackService()
        {
            if (AndroidTrackService.IsRunning)
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
            if (AndroidTrackService.IsRunning)
            {
                BindService(new Intent(this, typeof(AndroidTrackService)), trackServiceConnection, 0);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (AndroidTrackService.IsRunning)
            {
                UnbindService(trackServiceConnection);
            }
        }

        protected override void OnResume()
        {
            if (MainMapView.Current.LocationDisplay != null)
            {
                MainMapView.Current.LocationDisplay.IsEnabled = true;
            }
            base.OnResume();
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnStop()
        {
            MainMapView.Current.LocationDisplay.IsEnabled = false;
            base.OnStop();
        }
    }
}
