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
using MapBoard.IO;
using MapBoard.Mapping;

namespace MapBoard.Pages;

public partial class TrackPage : ContentPage
{
    public TrackPage()
    {
        InitializeComponent();
        BindingContext = new TrackPageViewModel();
    }

    private void StartTrackButton_Click(object sender, EventArgs e)
    {
        StartTrack();
    }

    private void StartTrack(string resume = null)
    {
        if (resume != null)
        {
            TrackService.ResumeGpx = resume;
        }
#if ANDROID
        (Platform.CurrentActivity as MainActivity).StartTrackService();
#else
        var trackService = new TrackService();
        trackService.Start();
#endif
        btnStart.IsVisible = false;
        btnStop.IsVisible = true;
    }

    private void Current_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void StopButton_Clicked(object sender, EventArgs e)
    {
#if ANDROID
        (Platform.CurrentActivity as MainActivity).StopTrackService();
#else
        TrackService.Current.Stop();
#endif
        btnStart.IsVisible = true;
        btnStop.IsVisible = false;
        //TrackService.Current.LocationChanged -= Current_LocationChanged;
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (TrackService.Current == null && Config.Instance.IsTracking)
        {
            var gpxFiles = Directory.EnumerateFiles(FolderPaths.TrackPath, "*.gpx").OrderDescending();
            if (gpxFiles.Any())
            {
                if (await DisplayAlert("继续记录轨迹", "轨迹记录意外退出，是否继续？", "继续", "取消"))
                {
                    StartTrack(gpxFiles.First());
                }
                else
                {
                    Config.Instance.IsTracking = false;
                }
            }
        }
    }

    private void ContentPage_Unloaded(object sender, EventArgs e)
    {
        Config.Instance.Save();
        MainMapView.Current.Layers.Save();
    }
}
