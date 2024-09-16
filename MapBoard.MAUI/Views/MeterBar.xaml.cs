using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using MapBoard.Mapping;
using MapBoard.Services;
using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class MeterBar : ContentView, ISidePanel
{
    public MeterBar()
    {
        InitializeComponent();
        BindingContext = new MeterBarViewModel();
    }

    public SwipeDirection Direction { get; }
    public int Length { get; }
    public bool Standalone => true;
    private bool canUpdateData = false;

    public void OnPanelOpening()
    {
        canUpdateData = true;
        Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            UpdateData();
            return canUpdateData;
        });
#if ANDROID
        (Platform.CurrentActivity as MainActivity).SetStatusBarVisible(false);
#endif
    }

    private void UpdateData()
    {
        var speed = 0d;
        string distance = "Î´Öª";
        if (TrackService.Current != null
            && TrackService.Current.LastLocation is Location location
            && location.Speed is double s)
        {
            speed = s;
            distance = (TrackService.Current.TotalDistance / 1000).ToString("0.0");
        }
        else if (MainMapView.Current is MainMapView m
            && m.LocationDisplay is LocationDisplay ld
            && ld.IsEnabled && ld.Started && ld.Location is Esri.ArcGISRuntime.Location.Location l)
        {
            speed = l.Velocity;
            distance = "Ðè¿ªÆô¹ì¼£";
        }
        (BindingContext as MeterBarViewModel).Speed = speed * 3.6;
        (BindingContext as MeterBarViewModel).Time = DateTime.Now;
        (BindingContext as MeterBarViewModel).Distance = distance;
    }

    public void OnPanelClosed()
    {
        canUpdateData = false;
#if ANDROID
        (Platform.CurrentActivity as MainActivity).SetStatusBarVisible(true);
#endif
    }
}