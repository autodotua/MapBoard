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
using Debug = System.Diagnostics.Debug;

namespace MapBoard
{
    [Activity(Theme = "@style/MyAppTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private bool hasPressedBack = false;
        private bool isActive = false;
        private int stopID = 0;
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
                    //if (AndroidTrackService.IsRunning)
                    //{
                    Finish();
                    //}
                    //else
                    //{
                    //    MapBoard.App.Current.Quit();
                    //}
                    //return;
                }

                else
                {
                    hasPressedBack = true;
                    Toast.MakeText(this, "再按一次退出应用", ToastLength.Short).Show();
                    await Task.Delay(2000);
                    hasPressedBack = false;
                }
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

        protected async override void OnDestroy()
        {
            Debug.WriteLine("MainActivity Destroy");
            base.OnDestroy();
            if (AndroidTrackService.IsRunning)
            {
                UnbindService(trackServiceConnection);
            }
            await MainMapView.Current.LocationDisplay.DataSource.StopAsync();
        }

        protected override void OnPause()
        {
            Debug.WriteLine("MainActivity Pause");
            base.OnPause();
            App.SaveConfigsAndStatus();
        }
        protected override void OnResume()
        {
            Debug.WriteLine("MainActivity Resume");
            base.OnResume();
        }
        protected override async void OnStart()
        {
            isActive = true;
            Debug.WriteLine("MainActivity Start");
            base.OnStart();
            if (MainMapView.Current.LocationDisplay != null
                && MainMapView.Current.LocationDisplay.DataSource.Status==Esri.ArcGISRuntime.Location.LocationDataSourceStatus.Stopped
                && MainMapView.Current.LocationDisplay.DataSource is LocationDataSourceAndroidImpl) //表示不是第一次打开了。第一次打开需要初始化。
            {
                await MainMapView.Current.LocationDisplay.DataSource.StartAsync();
            }
        }

        protected override async void OnStop()
        {
            isActive = false;
            Debug.WriteLine("MainActivity Stop");
            base.OnStop();
            if (TrackService.Current == null)
            {
                //如果开着轨迹记录，那么就不用停止LocationDisplay了，防止重新进去的时候又要重新定位
                await MainMapView.Current.LocationDisplay.DataSource.StopAsync();
            }

            int currentStopID = ++stopID;
            await Task.Delay(1000 * 60 * 1);
            if (Config.Instance.AutoQuit //开启自动退出
                && !isActive //Activity在后台
                && currentStopID == stopID //当前方法启动时和当前运行行之间，没有发生再一次的OnStop
                && AndroidTrackService.IsRunning == false) //没有在记录轨迹
            {
                MapBoard.App.Current.Quit();
            }
        }
    }
}
