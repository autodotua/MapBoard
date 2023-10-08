#if ANDROID
using Android.Content;
using Android.App;
using MapBoard.Platforms.Android;
using Application=Android.App.Application;
#endif
using MapBoard.Services;
using MapBoard.ViewModels;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace MapBoard.Pages;

public partial class TrackPage : ContentPage
{
    public TrackPage()
    {
        InitializeComponent();
    }

    private void StartTrackButton_Click(object sender, EventArgs e)
    {
#if ANDROID
        (Platform.CurrentActivity as MainActivity).StartTrackService();
#else
        TrackService trackService = new TrackService();
        trackService.Initialize();
        trackService.Start();
#endif
    }
}
