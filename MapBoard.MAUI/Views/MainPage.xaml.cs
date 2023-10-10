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

            layer.TranslationX = -300;
            layer.WidthRequest = 300;

            track.TranslationX = -300;
            track.WidthRequest = 300;

            ftp.TranslationX = -300;
            ftp.WidthRequest = 300;

            isOpened.Add(typeof(TrackView), false);
            isOpened.Add(typeof(LayerListView), false);
            isOpened.Add(typeof(FtpView), false);
            type2View.Add(typeof(TrackView), track);
            type2View.Add(typeof(LayerListView), layer);
            type2View.Add(typeof(FtpView), ftp);

            //大屏设备，底部操作栏在右下角悬浮
            if(DeviceInfo.Idiom!= DeviceIdiom.Phone)
            {
                grdMain.RowDefinitions.RemoveAt(1);
                bdBottom.Margin = new Thickness(16, 16);
                bdBottom.HorizontalOptions = LayoutOptions.End;
                bdBottom.VerticalOptions = LayoutOptions.End;
                bdBottom.WidthRequest = 200;
                bdBottom.StrokeShape = new RoundRectangle() { CornerRadius = new CornerRadius(8),Shadow=null };
            }
        }

        private Dictionary<Type,bool> isOpened= new Dictionary<Type,bool>();
        private Dictionary<Type,VisualElement> type2View= new Dictionary<Type, VisualElement>();


        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            if (Window != null)
            {
                Window.Title = "地图画板";
            }
            await CheckAndRequestLocationPermission();
        }

        public static MainPage Current { get; private set; }

        public void OpenOrClosePanel<T>()
        {
            Type type = typeof(T);
            if (isOpened[type])
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
           var type=typeof(T);
            if (isOpened.Values.Any(p => p))
            {
                foreach (var key in isOpened.Keys)
                {
                    if (isOpened[key])
                    {
                        ClosePanel(type2View[key]);
                        isOpened[key] = false;
                    }
                }
            }
            type2View[type].TranslateTo(0, 0);
            isOpened[type] = true;
        }
        public void ClosePanel<T>()
        {
            var type = typeof(T);
            ClosePanel(type2View[type]);
            isOpened[type] = false;
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
                    await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "确定");
                }
                else
                {
                    await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "进入设置");
                    AppInfo.ShowSettingsUI();
                    return;
                }
                await RequestAsync<LocationAlways>();
            }

            while ((await CheckStatusAsync<LocationWhenInUse>()) != PermissionStatus.Granted)
            {
                if (ShouldShowRationale<LocationWhenInUse>())
                {
                    await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "确定");
                }
                else
                {
                    await Shell.Current.DisplayAlert("需要权限", "该应用需要定位权限，否则无法正常工作", "进入设置");
                    AppInfo.ShowSettingsUI();
                    return;
                }
                await RequestAsync<LocationWhenInUse>();
            }

#if ANDROID
        if ((await Permissions.CheckStatusAsync<AndroidNotificationPermission>()) != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<AndroidNotificationPermission>();
        }
#endif
        }
    }

}
