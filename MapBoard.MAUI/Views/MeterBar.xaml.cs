using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using MapBoard.Mapping;
using MapBoard.Services;
using MapBoard.Util;
using MapBoard.ViewModels;
using System.Diagnostics;

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
        MainMapView.Current.LocationDisplay.LocationChanged += LocationDisplay_LocationChanged;

        MeterBarViewModel vm = BindingContext as MeterBarViewModel;
        lastUpdateTime = DateTime.MaxValue;
        windowDistance = 0;
        sumDistance = 0;
        qTimes.Clear();
        qDistances.Clear();
        lastLocation = null;
        vm.Distance = 0;
        vm.Speed = 0;

        //在一个点停留一定时间后，速度归零
        Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            if (!canUpdateData)
            {
                return false;
            }
            DateTime now = DateTime.Now;
            if ((DateTime.Now - lastUpdateTime).TotalSeconds > 3)
            {
                Debug.WriteLine("停留过久");
                Update(lastLocation);
            }
            vm.Time = now;
            return true;
        });

#if ANDROID
        (Platform.CurrentActivity as MainActivity).SetStatusBarVisible(false);
#endif
    }

    private void LocationDisplay_LocationChanged(object sender, Esri.ArcGISRuntime.Location.Location e)
    {
        Debug.WriteLine("位置更新");
        lastUpdateTime = DateTime.Now;
        Update(e.Position);
    }

    private void Update(MapPoint location)
    {
        Debug.Assert(qTimes.Count == 0 || qTimes.Count == qDistances.Count + 1);
        try
        {
            var now = DateTime.Now;
            qTimes.Enqueue(now);

            //第1次变化
            if (lastLocation == null)
            {
                return;
            }
            var distance = GeometryUtility.GetDistance(location, lastLocation);
            Debug.WriteLine($"distance={distance}");
            qDistances.Enqueue(distance);
            windowDistance += distance;
            sumDistance += distance;
            var speed = windowDistance / (now - qTimes.Peek()).TotalSeconds * 3.6; //m/s=>km/h
            if (speed < 0.06)
            {
                speed = 0;
            }


            if (qTimes.Count > pointCountInCaculating)
            {
                qTimes.Dequeue();
                windowDistance -= qDistances.Dequeue();
            }

            MeterBarViewModel vm = BindingContext as MeterBarViewModel;
            vm.Speed = speed;
            vm.Distance = sumDistance / 1000;
        }
        finally
        {
            lastLocation = location;
        }
    }

    private MapPoint lastLocation;
    private double windowDistance = 0;
    private double sumDistance = 0;
    private readonly int pointCountInCaculating = 5;
    private Queue<DateTime> qTimes = new Queue<DateTime>();
    private Queue<double> qDistances = new Queue<double>();
    private DateTime lastUpdateTime = DateTime.MaxValue;

    public void OnPanelClosed()
    {
        canUpdateData = false;
        MainMapView.Current.LocationDisplay.LocationChanged -= LocationDisplay_LocationChanged;

#if ANDROID
        (Platform.CurrentActivity as MainActivity).SetStatusBarVisible(true);
#endif
    }
}