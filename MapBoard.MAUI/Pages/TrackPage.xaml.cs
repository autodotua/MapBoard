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
    TrackService trackService;
    public TrackPage()
    {
        InitializeComponent();
    }

    private void StartTrackButton_Click(object sender, EventArgs e)
    {
#if ANDROID
        (Platform.CurrentActivity as MainActivity).StartTrackService();
#else
        trackService = new TrackService();
        trackService.Start();
#endif
        btnStart.IsVisible = false;
        btnStop.IsVisible = true;
    }


    private void StopButton_Clicked(object sender, EventArgs e)
    {
#if ANDROID
        (Platform.CurrentActivity as MainActivity).StopTrackService();
#else
        trackService.Stop();
#endif
        btnStart.IsVisible = true;
        btnStop.IsVisible = false;
    }
}
