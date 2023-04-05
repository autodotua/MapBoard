using System;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Geometry;
using FzLib;
using MapBoard.Extension;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using MapBoard.Mapping;
using System.ComponentModel;
using ModernWpf.FzExtension.CommonDialog;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Util;
using Esri.ArcGISRuntime.UI.Controls;

namespace MapBoard.UI.Extension
{
    /// <summary>
    /// 地理逆编码面板
    /// </summary>
    public partial class ReGeoCodePanel : ExtensionPanelBase
    {
        private int radius = 1000;

        private PoiInfo selectedPoi;

        public ReGeoCodePanel()
        {
            if (ExtensionUtility.ReGeoCodeEngines.Count > 0)
            {
                SelectedReGeoCodeEngine = ExtensionUtility.ReGeoCodeEngines[0];
            }
            InitializeComponent();
        }

        /// <summary>
        /// 搜索点
        /// </summary>
        public Location Point { get; set; }

        /// <summary>
        /// 搜索半径
        /// </summary>
        public int Radius
        {
            get => radius;
            set
            {
                if (value < 1)
                {
                    radius = 1;
                }
                else if (value > 3000)
                {
                    radius = 3000;
                }
                radius = value;
            }
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        public LocationInfo SearchResult { get; set; }

        /// <summary>
        /// 选中的POI
        /// </summary>
        public PoiInfo SelectedPoi
        {
            get => selectedPoi;
            set
            {
                selectedPoi = value;
                Overlay.SelectPoi(value);
            }
        }

        /// <summary>
        /// 使用的搜索引擎
        /// </summary>
        public IReGeoCodeEngine SelectedReGeoCodeEngine { get; set; }

        /// <summary>
        /// 单击选择点按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ChoosePointButton_Click(object sender, RoutedEventArgs e)
        {
            MapPoint point = await GetPointAsync();
            if (point != null)
            {
                point = GeometryEngine.Project(point, SpatialReferences.Wgs84) as MapPoint;
                Point = point.ToLocation();
                Overlay.ShowLocation(point);
            }
        }

        /// <summary>
        /// 单击清除搜索结果按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            Overlay.ClearLocation();
            Overlay.ClearPois();
            SearchResult = null;
        }

        /// <summary>
        /// 单击设置为当前设备位置点按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocationPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (MapView is MapView m)
            {
                if (m.LocationDisplay.Location == null)
                {
                    CommonDialog.ShowErrorDialogAsync("目前还未成功定位");
                    return;
                }
                MapPoint point = GeometryEngine.Project(m.LocationDisplay.MapLocation, SpatialReferences.Wgs84) as MapPoint;

                Point = point.ToLocation();
            }
            else
            {
                CommonDialog.ShowErrorDialogAsync("不支持该地图");
            }
        }

        /// <summary>
        /// 位置文本框如果得到焦点，就需要保证<see cref="Point"/>不为空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocationTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Point ??= new Location();
        }

        /// <summary>
        /// 单击搜索按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedReGeoCodeEngine == null)
            {
                await CommonDialog.ShowErrorDialogAsync("没有选择任何POI搜索引擎");
                return;
            }
            if (Point == null || Point.Longitude * Point.Latitude == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("请先设置点的经纬度");
                return;
            }
            if ((sender as Button).IsEnabled == false)
            {
                return;
            }
            try
            {
                (sender as Button).IsEnabled = false;

                SearchResult = await SelectedReGeoCodeEngine.SearchAsync(Point.ToMapPoint(), Radius);
                Overlay.ShowPois(SearchResult.Pois);
            }
            catch (Exception ex)
            {
                App.Log.Error("搜索失败", ex);
                await CommonDialog.ShowErrorDialogAsync(ex, "搜索失败");
            }
            finally
            {
                (sender as Button).IsEnabled = true;
            }
        }

        /// <summary>
        /// 文本框按Enter进行搜索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }
    }
}