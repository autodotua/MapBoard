#if ANDROID
using Android.Content;
using Android.App;
using MapBoard.Platforms.Android;
using Application = Android.App.Application;
#endif
using MapBoard.Services;
using MapBoard.ViewModels;
using System.Globalization;
using System.Linq;
using Microsoft.Maui.Controls.PlatformConfiguration;
using MapBoard.IO;
using MapBoard.Mapping;
using MapBoard.IO.Gpx;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Models;

namespace MapBoard.Views;

public partial class TrackView : ContentView, ISidePanel
{
    public TrackView()
    {
        InitializeComponent();
        BindingContext = new TrackViewViewModel();

        //轨迹记录有异常，在此处处理
        TrackService.ExceptionThrown += TrackService_ExceptionThrown;

        //处理TrackService.Current发生改变，即轨迹记录的开始和停止事件
        TrackService.CurrentChanged += TrackService_StaticPropertyChanged;

        //轨迹记录结束、GPX保存后，更新GPX文件列表
        TrackService.GpxSaved += TrackService_GpxSaved;
    }

    public void OnPanelClosed()
    {
    }

    public async void OnPanelOpening()
    {
        await (BindingContext as TrackViewViewModel).LoadGpxFilesAsync();
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        UpdateButtonsVisible(TrackService.Current != null);
    }

    private void ContentPage_Unloaded(object sender, EventArgs e)
    {
        Config.Instance.Save();
        MainMapView.Current.Layers.Save();
    }

    private async void GpxList_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        var file = e.Item as SimpleFile;
        await LoadGpxAsync(file.FullName);
    }

    private async Task LoadGpxAsync(string path)
    {
        IsEnabled = false;
        try
        {
            Gpx gpx = await Gpx.FromFileAsync(path);

            var overlay = MainMapView.Current.TrackOverlay;
            var extent = await overlay.LoadColoredGpxAsync(gpx);
            MainPage.Current.ClosePanel<TrackView>();
            await MainMapView.Current.ZoomToGeometryAsync(extent);
        }
        catch (Exception ex)
        {
            await MainPage.Current.DisplayAlert("加载失败", ex.Message, "确定");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private async void ResumeButton_Clicked(object sender, EventArgs e)
    {
        var gpxs = (BindingContext as TrackViewViewModel).GpxFiles;
        if (!gpxs.Any())
        {
            await MainPage.Current.DisplayAlert("无法继续", "不存在现有的轨迹", "确定");
            return;
        }
        StartTrack(gpxs[0].FullName);
        //UpdateButtonsVisible(true);
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
        MainPage.Current.ClosePanel<TrackView>();
    }

    private async void StartTrackButton_Click(object sender, EventArgs e)
    {
        try
        {
            Config.Instance.IsTracking = true;
            StartTrack();
            //UpdateButtonsVisible(true);
        }
        catch (Exception ex)
        {
            await MainPage.Current.DisplayAlert("开始记录轨迹失败", ex.Message, "确定");
        }
    }

    private void StopButton_Clicked(object sender, EventArgs e)
    {
        StopTrack();
        //UpdateButtonsVisible(false);
    }

    private void StopTrack()
    {
#if ANDROID
        (Platform.CurrentActivity as MainActivity).StopTrackService();
#else
        TrackService.Current.Stop();
#endif
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        PickOptions options = new PickOptions()
        {
            PickerTitle = "选取GPX轨迹文件",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>()
            {
                [DevicePlatform.Android] = new[] { "application/gpx", "application/gpx+xml", "application/octet-stream" },
                [DevicePlatform.WinUI] = new[] { "*.gpx" }
            })
        };
        var file = await FilePicker.Default.PickAsync(options);
        if (file != null)
        {
            await LoadGpxAsync(file.FullPath);
        }
    }

    private void TrackService_ExceptionThrown(object sender, ThreadExceptionEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (MainPage.Current.IsVisible)
            {
                MainPage.Current.DisplayAlert("轨迹记录失败", e.Exception.Message, "确定");
                UpdateButtonsVisible(false);
            }
        });
    }

    private async void TrackService_GpxSaved(object sender, TrackService.GpxSavedEventArgs e)
    {
        var gpxs = (BindingContext as TrackViewViewModel)?.GpxFiles;
        if (gpxs != null && (gpxs.Count == 0 || gpxs[0].FullName != e.FilePath))
        {
            gpxs.Insert(0, new Models.SimpleFile(e.FilePath));
        }
        if (TrackService.Current == null)
        {
            await LoadGpxAsync(e.FilePath);
        }
    }

    private void TrackService_StaticPropertyChanged(object sender, EventArgs e)
    {
        MainThread.InvokeOnMainThreadAsync(() =>
        {
            UpdateButtonsVisible(TrackService.Current != null);
        });
    }

    private void UpdateButtonsVisible(bool running)
    {
        btnStart.IsVisible = !running;
        btnStop.IsVisible = running;
        btnResume.IsVisible = !running;
        btnResume.IsEnabled = (BindingContext as TrackViewViewModel).GpxFiles.Count > 0;
        grdDetail.IsVisible = running;
        lvwGpxList.IsVisible = !running;
        lblLoadGpx.IsVisible = !running;
    }
}
