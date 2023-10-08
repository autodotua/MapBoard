using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MapBoard
{
    [Activity(Theme = "@style/MyAppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity Current { get; private set; }
        public MainActivity()
        {
            Current = this;
        }
    }
}
