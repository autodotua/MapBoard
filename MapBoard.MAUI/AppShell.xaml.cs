using MapBoard.Pages;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace MapBoard;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("LayerPage", typeof(LayerPage));
        if (true || DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            CurrentItem = tab;
        }
    }

    public async Task CheckAndRequestLocationPermission()
    {

        while ((await CheckStatusAsync<LocationAlways>()) != PermissionStatus.Granted)
        {
            if (ShouldShowRationale<LocationAlways>())
            {
                await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "确定");
            }
            else
            {
                await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "进入设置");
                AppInfo.ShowSettingsUI();
                return;
            }
            await RequestAsync<LocationAlways>();

        }

#if ANDROID
        if ((await Permissions.CheckStatusAsync<NotificationPermission>()) != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<NotificationPermission>();
        }
#endif
    }


    private async void Shell_Loaded(object sender, EventArgs e)
    {
        await CheckAndRequestLocationPermission();
    }
}
#if ANDROID
public class NotificationPermission : BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
      new List<(string androidPermission, bool isRuntime)>
      {
        (global::Android.Manifest.Permission.PostNotifications, true),
      }.ToArray();
}
#endif
