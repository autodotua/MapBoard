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
using System.Windows;
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
        public MainPage()
        {
            if (Current != null)
            {
                throw new Exception("仅允许一个实例");
            }
            Current = this;
            InitializeComponent();


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
                bdBottom.WidthRequest = 200;
                bdBottom.StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(8), Shadow = null };
            }
        }

        private SidePanelInfo[] sidePanels;
        private Dictionary<Type, SidePanelInfo> type2SidePanels;

        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            if (Window != null)
            {
                Window.Title = "地图画板";
            }

#if ANDROID
            var height = (Platform.CurrentActivity as MainActivity).GetNavBarHeight();
            height /= DeviceDisplay.MainDisplayInfo.Density;
            if (height > 0)
            {
                bdBottom.Padding = new Thickness(bdBottom.Padding.Left, bdBottom.Padding.Top, bdBottom.Padding.Right, bdBottom.Padding.Bottom + height);
            }
#endif

            await CheckAndRequestLocationPermission();
        }

        public static MainPage Current { get; private set; }

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
            if (sidePanels.Any(p => p.IsOpened && !p.Standalone))
            {
                foreach (var panel in sidePanels.Where(p => p.IsOpened && !p.Standalone))
                {
                    ClosePanel(panel.Instance);
                    panel.IsOpened = false;
                }
            }
            type2SidePanels[type].Instance.TranslateTo(0, 0);
            type2SidePanels[type].IsOpened = true;
        }
        public void ClosePanel<T>()
        {
            var type = typeof(T);
            ClosePanel(type2SidePanels[type].Instance);
            type2SidePanels[type].IsOpened = false;
        }

        private void ClosePanel(VisualElement element)
        {
            element.TranslateTo(-300, 0);
        }

        private void CloseLayerPanelButton_Click(object sender, EventArgs e)
        {
            ClosePanel<LayerListView>();
        }

        private void TrackButton_Clicked(object sender, EventArgs e)
        {
            OpenOrClosePanel<TrackView>();
        }

        private void FtpButton_Clicked(object sender, EventArgs e)
        {
            OpenOrClosePanel<FtpView>();
        }
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

        private async void RefreshButton_Clicked(object sender, EventArgs e)
        {
            await MainMapView.Current.LoadAsync();
        }
    }

}
