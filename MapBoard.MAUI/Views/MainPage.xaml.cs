using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Maui;
using MapBoard.Mapping;
using MapBoard.Model;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using FzLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Map = Esri.ArcGISRuntime.Mapping.Map;
using Esri.ArcGISRuntime.UI;
using static MapBoard.Util.GeometryUtility;
using FubarDev.FtpServer.FileSystem.DotNet;
using FubarDev.FtpServer;
using MapBoard.Services;
using MapBoard.Views;
using static Microsoft.Maui.ApplicationModel.Permissions;
using Microsoft.Maui.Controls.Shapes;


#if ANDROID
using MapBoard.Platforms.Android;
#endif

namespace MapBoard.Views
{
    public partial class MainPage : ContentPage
    {
        private SidePanelInfo[] sidePanels;

        private Dictionary<Type, SidePanelInfo> type2SidePanels;
        private void HandleRippleBug()
        {
#if ANDROID
            //Microsoft.Maui.Handlers.ImageButtonHandler.Mapper.AppendToMapping("WhiteBackground", (handler, view) =>
            //{
            //    var button = view.Handler.PlatformView as Google.Android.Material.ImageView.ShapeableImageView;
            //    //button.SetBackgroundColor(Android.Graphics.Color.White);
            //    button.SetBackgroundDrawable(new RippleDrawable(ColorStateList.ValueOf(Android.Graphics.Color.Gray), null, null));
            //});    
            //Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("WhiteBackgroundButton", (handler, view) =>
            //{
            //    var button = view.Handler.PlatformView as AndroidX.AppCompat.Widget.AppCompatButton;
            //    //button.SetBackgroundColor(Android.Graphics.Color.White);
            //    button.SetBackgroundColor(Android.Graphics.Color.White);
            //});
#endif
        }

        public MainPage()
        {
            if (Current != null)
            {
                throw new Exception("仅允许一个实例");
            }
            Current = this;
            InitializeComponent();
            HandleRippleBug();

            sidePanels =
            [
                new SidePanelInfo
                {
                    Type = typeof(LayerListView),
                    Direction = SwipeDirection.Left,
                    Instance = layer,
                    Length = 300,
                },
                new SidePanelInfo
                {
                    Type = typeof(TrackView),
                    Direction = SwipeDirection.Left,
                    Instance = track,
                    Length = 300,
                },
                new SidePanelInfo
                {
                    Type = typeof(FtpView),
                    Direction = SwipeDirection.Left,
                    Instance = ftp,
                    Length = 300,
                },
                new SidePanelInfo
                {
                    Type = typeof(BaseLayerView),
                    Direction = SwipeDirection.Left,
                    Instance = baseLayer,
                    Length = 300,
                },
                new SidePanelInfo
                {
                    Type = typeof(ImportView),
                    Direction = SwipeDirection.Left,
                    Instance = import,
                    Length = 300,
                },
                new SidePanelInfo
                {
                    Type = typeof(TrackingBar),
                    Direction = SwipeDirection.Up,
                    Instance = tbar,
                    Length = 96,
                    Standalone = true,
                },
            ];
            type2SidePanels = sidePanels.ToDictionary(p => p.Type);

            foreach (var panel in sidePanels)
            {
                switch (panel.Direction)
                {
                    case SwipeDirection.Right:
                        panel.Instance.WidthRequest = panel.Length;
                        panel.Instance.TranslationX = panel.Length;
                        break;
                    case SwipeDirection.Left:
                        panel.Instance.WidthRequest = panel.Length;
                        panel.Instance.TranslationX = -panel.Length;
                        break;
                    case SwipeDirection.Up:
                        panel.Instance.HeightRequest = panel.Length;
                        panel.Instance.TranslationY = -panel.Length;
                        break;
                    case SwipeDirection.Down:
                        panel.Instance.HeightRequest = panel.Length;
                        panel.Instance.TranslationY = panel.Length;
                        break;
                    default:
                        break;
                }
            }

            //大屏设备，底部操作栏在右下角悬浮
            if (DeviceInfo.Idiom != DeviceIdiom.Phone)
            {
                grdMain.RowDefinitions.RemoveAt(1);
                bdBottom.Margin = new Thickness(16, 16);
                bdBottom.HorizontalOptions = LayoutOptions.End;
                bdBottom.VerticalOptions = LayoutOptions.End;
                bdBottom.WidthRequest = (bdBottom.Content as Microsoft.Maui.Controls.Grid).Children.Count * 60;
                bdBottom.StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(8), Shadow = null };
            }

            TrackService.CurrentChanged += TrackService_CurrentChanged;
        }

        public static MainPage Current { get; private set; }

        public async Task CheckAndRequestLocationPermission()
        {
            while ((await CheckStatusAsync<LocationAlways>()) != PermissionStatus.Granted)
            {
                if (ShouldShowRationale<LocationAlways>())
                {
                    await DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "确定");
                }
                else
                {
                    await DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "进入设置");
                    AppInfo.ShowSettingsUI();
                    return;
                }
                await RequestAsync<LocationAlways>();
            }

            while ((await CheckStatusAsync<LocationWhenInUse>()) != PermissionStatus.Granted)
            {
                if (ShouldShowRationale<LocationWhenInUse>())
                {
                    await DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "确定");
                }
                else
                {
                    await DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "进入设置");
                    AppInfo.ShowSettingsUI();
                    return;
                }
                await RequestAsync<LocationWhenInUse>();
            }

#if ANDROID
            if ((await CheckStatusAsync<AndroidNotificationPermission>()) != PermissionStatus.Granted)
            {
                await RequestAsync<AndroidNotificationPermission>();
            }
#endif
        }

        public void ClosePanel<T>()
        {
            ClosePanel(typeof(T));
        }

        public void OpenOrClosePanel<T>()
        {
            Type type = typeof(T);
            if (type2SidePanels[type].IsOpened)
            {
                ClosePanel<T>();
            }
            else
            {
                OpenPanel<T>();
            }
        }

        public void OpenPanel<T>()
        {
            var type = typeof(T);
            CloseAllPanel();
            type2SidePanels[type].Instance.TranslateTo(0, 0);
            type2SidePanels[type].IsOpened = true;
        }

        private void CloseAllPanel()
        {
            if (sidePanels.Any(p => p.IsOpened && !p.Standalone))
            {
                foreach (var panel in sidePanels.Where(p => p.IsOpened && !p.Standalone))
                {
                    ClosePanel(panel.Type);
                    panel.IsOpened = false;
                }
            }
        }

        private void ClosePanel(Type type)
        {
            var panel = type2SidePanels[type];
            switch (panel.Direction)
            {
                case SwipeDirection.Right:
                    panel.Instance.TranslateTo(panel.Length, 0);
                    break;
                case SwipeDirection.Left:
                    panel.Instance.TranslateTo(-panel.Length, 0);
                    break;
                case SwipeDirection.Up:
                    panel.Instance.TranslateTo(0, -panel.Length);
                    break;
                case SwipeDirection.Down:
                    panel.Instance.TranslateTo(0, panel.Length);
                    break;
            }
            type2SidePanels[type].IsOpened = false;
        }

        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            if (Window != null)
            {
                Window.Title = "地图画板";
            }
            MainMapView.Current.GeoViewTapped += (s, e) =>
            {
                CloseAllPanel();
            };
#if ANDROID
            var height = (Platform.CurrentActivity as MainActivity).GetNavBarHeight();
            height /= (DeviceDisplay.MainDisplayInfo.Density * 2);
            if (height > 0)
            {
                bdBottom.Padding = new Thickness(bdBottom.Padding.Left, bdBottom.Padding.Top, bdBottom.Padding.Right, bdBottom.Padding.Bottom + height);
            }
#endif

            await CheckAndRequestLocationPermission();
        }

        private void FtpButton_Clicked(object sender, EventArgs e)
        {
            OpenOrClosePanel<FtpView>();
        }

        private void ImportButton_Clicked(object sender, EventArgs e)
        {
            OpenOrClosePanel<ImportView>();
        }

        private void LayerButton_Click(object sender, EventArgs e)
        {
            OpenOrClosePanel<LayerListView>();
        }

        private async void RefreshButton_Clicked(object sender, EventArgs e)
        {
            await MainMapView.Current.LoadAsync();
        }

        private void TrackButton_Clicked(object sender, EventArgs e)
        {
            OpenOrClosePanel<TrackView>();
        }

        private void TrackService_CurrentChanged(object sender, EventArgs e)
        {
            if (TrackService.Current == null)
            {
                ClosePanel<TrackingBar>();
            }
            else
            {
                OpenPanel<TrackingBar>();
            }
        }
        private async void ZoomToLayerButton_Click(object sender, EventArgs e)
        {
            var layer = MainMapView.Current?.Layers?.Selected;
            if (layer != null)
            {
                try
                {
                    var extent = await layer.QueryExtentAsync(new QueryParameters());
                    await MainMapView.Current.ZoomToGeometryAsync(extent);
                }
                catch { }
            }
        }

        private void SetBaseLayersTapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            OpenPanel<BaseLayerView>();
        }
    }

}
