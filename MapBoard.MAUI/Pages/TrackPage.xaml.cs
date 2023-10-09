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
                    UpdateButtonsVisible(true, false);
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

    private void Current_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void PauseButton_Clicked(object sender, EventArgs e)
    {
        StopTrack(true);
        UpdateButtonsVisible(true, true);
    }

    private void ResumeButton_Clicked(object sender, EventArgs e)
    {
        StartTrack();
        UpdateButtonsVisible(true, false);
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
    }

    private void StartTrackButton_Click(object sender, EventArgs e)
    {
        Config.Instance.IsTracking = true;
        StartTrack();
        UpdateButtonsVisible(true, false);
    }
    private void StopButton_Clicked(object sender, EventArgs e)
    {
        if (TrackService.Current == null) //暂停后停止
        {
            Config.Instance.IsTracking = false;
        }
        else //直接停止
        {
            StopTrack(false);
        }
        UpdateButtonsVisible(false, false);
    }

    private void StopTrack(bool pause)
    {
        if (pause)
        {
            TrackService.Current.PutPausingFlag();
        }
#if ANDROID
        (Platform.CurrentActivity as MainActivity).StopTrackService();
#else
        TrackService.Current.Stop();
#endif
    }
    private void UpdateButtonsVisible(bool running,bool pausing)
    {
        btnStart.IsVisible = !running;
        grdStopAndPause.IsVisible = running;
        btnPause.IsVisible = !pausing;   
        btnResume.IsVisible = pausing;   
        lblPausing.IsVisible = pausing;
    }
}
