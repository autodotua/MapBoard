using MapBoard.Pages;

namespace MapBoard
{
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
            while (( await Permissions.CheckStatusAsync<Permissions.LocationAlways>()) != PermissionStatus.Granted)
            {
                if (Permissions.ShouldShowRationale<Permissions.LocationAlways>())
                {
                    await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "确定");
                }
                else
                {
                    await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "进入设置");
                    AppInfo.ShowSettingsUI();
                    return;
                }
                await Permissions.RequestAsync<Permissions.LocationAlways>();
            }
        }

        private async void Shell_Loaded(object sender, EventArgs e)
        {
            await CheckAndRequestLocationPermission();
        }
    }
}
