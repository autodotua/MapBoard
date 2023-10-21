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
using MapBoard.Mapping.Model;
using System.Xml.Linq;
using MapBoard.IO;
using CommunityToolkit.Maui.Alerts;
using static MapBoard.Views.PopupMenu;





#if ANDROID
using MapBoard.Platforms.Android;
#endif

namespace MapBoard.Views
{
    public partial class MainPage : ContentPage
    {
        private SidePanelInfo[] sidePanels;

        private Dictionary<Type, SidePanelInfo> type2SidePanels;
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
                new SidePanelInfo(layer, layerView),
                new SidePanelInfo(track, trackView),
                new SidePanelInfo(ftp, ftpView),
                new SidePanelInfo(baseLayer, baseLayerView),
                new SidePanelInfo(import, importView),
                new SidePanelInfo(tbar, tbar),
                new SidePanelInfo(ebar, ebar)
            ];
            type2SidePanels = sidePanels.ToDictionary(p => p.Type);

            foreach (var panel in sidePanels)
            {
                switch (panel.Direction)
                {
                    case SwipeDirection.Right:
                        panel.Container.WidthRequest = panel.Length;
                        panel.Container.TranslationX = panel.Length;
                        break;
                    case SwipeDirection.Left:
                        panel.Container.WidthRequest = panel.Length;
                        panel.Container.TranslationX = -panel.Length;
                        break;
                    case SwipeDirection.Up:
                        panel.Container.HeightRequest = panel.Length;
                        panel.Container.TranslationY = -panel.Length;
                        break;
                    case SwipeDirection.Down:
                        panel.Container.HeightRequest = panel.Length;
                        panel.Container.TranslationY = panel.Length;
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

        public async void OpenPanel<T>()
        {
            var type = typeof(T);
            CloseAllPanel();
            type2SidePanels[type].Content.OnPanelOpening();
            type2SidePanels[type].IsOpened = true;
            if (!await type2SidePanels[type].Container.TranslateTo(0, 0))
            {
                type2SidePanels[type].Content.OnPanelOpened();
            }
        }

        private async void AddGeometryButton_Clicked(object sender, EventArgs e)
        {
            if (MainMapView.Current.Editor.IsEditing)
            {
                return;
            }
            CloseAllPanel();
            var layers = MainMapView.Current.Layers
                .OfType<IMapLayerInfo>()
                .Where(p => p.LayerVisible)
                .Where(p => p.CanEdit)
                .Select(p => new PopupMenuItem(p.Name) { Tag = p })
                .ToList();
            if (layers.Count == 0)
            {
                await Toast.Make("不存在任何可见可编辑图层").Show();
                return;
            }
            var result = await (sender as View).PopupMenuAsync(layers, "选择图层");
            if (result >= 0)
            {
                MainMapView.Current.Editor.StartDraw(layers[result].Tag as IMapLayerInfo);
            }
        }

        private async Task CheckCrashAsync()
        {
            var file = Directory.EnumerateFiles(FolderPaths.LogsPath, "Crash*.log")
                .OrderByDescending(p => p)
                .FirstOrDefault();
            if (file != null && file != Config.Instance.LastCrashFile)
            {
                Config.Instance.LastCrashFile = file;
                Config.Instance.Save();
                await DisplayAlert("上一次崩溃", File.ReadAllText(file), "确定");
            }
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
                    panel.Container.TranslateTo(panel.Length, 0);
                    break;
                case SwipeDirection.Left:
                    panel.Container.TranslateTo(-panel.Length, 0);
                    break;
                case SwipeDirection.Up:
                    panel.Container.TranslateTo(0, -panel.Length);
                    break;
                case SwipeDirection.Down:
                    panel.Container.TranslateTo(0, panel.Length);
                    break;
            }
            type2SidePanels[type].IsOpened = false;
            if (type2SidePanels[type].Content is ISidePanel s)
            {
                s.OnPanelClosed();
            }
        }

        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            if (Window != null)
            {
                Window.Title = "地图画板";
            }
            MainMapView.Current.GeoViewTapped += (s, e) => CloseAllPanel();
            MainMapView.Current.BoardTaskChanged += MapView_BoardTaskChanged;
#if ANDROID
            var height = (Platform.CurrentActivity as MainActivity).GetNavBarHeight();
            height /= (DeviceDisplay.MainDisplayInfo.Density * 2);
            if (height > 0)
            {
                bdBottom.Padding = new Thickness(bdBottom.Padding.Left, bdBottom.Padding.Top, bdBottom.Padding.Right, bdBottom.Padding.Bottom + height);
            }
#endif

            await CheckAndRequestLocationPermission();

            await CheckCrashAsync();
        }

        private void Current_SelectedFeatureChanged(object sender, EventArgs e)
        {

        }

        private void FtpTapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            OpenOrClosePanel<FtpView>();
        }

        private void ImportButton_Clicked(object sender, EventArgs e)
        {
            OpenOrClosePanel<ImportView>();
        }

        private void LayerButton_Click(object sender, EventArgs e)
        {
            if (MainMapView.Current.CurrentTask == BoardTask.Ready)
            {
                OpenOrClosePanel<LayerListView>();
            }
        }

        private void MapView_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask is BoardTask.Select or BoardTask.Draw)
            {
                OpenPanel<EditBar>();
            }
            else
            {
                ClosePanel<EditBar>();
            }
        }
        private async void RefreshButton_Clicked(object sender, EventArgs e)
        {
            var handle = ProgressPopup.Show("正在重载");
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            try
            {
                await MainMapView.Current.LoadAsync();
            }
            finally
            {
                handle.Close();
            }
        }

        private void SetBaseLayersTapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            OpenPanel<BaseLayerView>();
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
            var layers = MainMapView.Current.Layers.Where(p => p.LayerVisible).ToDictionary(p => p.Name);
            if (layers.Count != 0)
            {
                List<PopupMenuItem> items = [new PopupMenuItem("全部")];
                foreach (var name in layers.Keys)
                {
                    items.Add(new PopupMenuItem(name));
                }
                var result = await (sender as View).PopupMenuAsync(items, "缩放到图层");
                //string result = await DisplayActionSheet("缩放到图层", "取消", "全部", layers.Keys.ToArray());
                if (result >= 0)
                {
                    Envelope extent = null;
                    var handle = ProgressPopup.Show("正在查找范围");
                    try
                    {
                        if (result > 0)
                        {
                            var name = items[result].Text;
                            var layer = layers[name];
                            extent = await (layer as IMapLayerInfo).QueryExtentAsync(new QueryParameters());
                        }
                        else
                        {
                            EnvelopeBuilder eb = new EnvelopeBuilder(SpatialReferences.Wgs84);
                            foreach (var layer2 in layers.Values)
                            {
                                var tempExtent = await (layer2 as IMapLayerInfo).QueryExtentAsync(new QueryParameters());
                                eb.UnionOf(tempExtent);
                            }
                            extent = eb.ToGeometry();
                        }
                        handle.Close();
                        await MainMapView.Current.ZoomToGeometryAsync(extent);
                    }
                    catch (Exception ex)
                    {
                        handle.Close();
                        await DisplayAlert("错误", ex.Message, "确定");
                    }
                }
            }
        }
    }

}
