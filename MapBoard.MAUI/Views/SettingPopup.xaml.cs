using CommunityToolkit.Maui.Views;
using Esri.ArcGISRuntime.Data;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class SettingPopup : Popup
{
    public SettingPopup()
    {
        BindingContext = Config.Instance;
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
        Config.Instance.Save();
        Close();
    }

    protected override Task OnDismissedByTappingOutsideOfPopup()
    {
        Config.Instance.Save();
        return base.OnDismissedByTappingOutsideOfPopup();
    }

    private async void AboutButton_Click(object sender, EventArgs e)
    {
        Uri uri = new Uri("https://github.com/autodotua/MapBoard");
        await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
    }

    private void MeasureLengthButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.Editor.StartMeasureLength();
        Close();
    }

    private void MeasureAreaButton_Click(object sender, EventArgs e)
    {
        MainMapView.Current.Editor.StartMeasureArea();
        Close();
    }
}