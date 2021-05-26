using FzLib.WPF.Dialog;
using FzLib.WPF.Extension;
using MapBoard.Common;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class BaseLayerSettingDialog : Common.DialogWindowBase
    {
        public Config Config => Config.Instance;

        public BaseLayerSettingDialog(ArcMapView mapView)
        {
            BaseLayers = new ObservableCollection<BaseLayerInfo>(
                Config.Instance.BaseLayers.Select(p => p.Clone()));
            ResetIndex();
            BaseLayers.CollectionChanged += (p1, p2) => ResetIndex();
            InitializeComponent();
            new DataGridHelper<BaseLayerInfo>(grd).EnableDragAndDropItem();
            MapView = mapView;
        }

        private void ResetIndex()
        {
            for (int i = 0; i < BaseLayers.Count; i++)
            {
                BaseLayers[i].Index = i + 1;
            }
            //grd.Columns[0].SortMemberPath = "Index";
            //grd.Columns[0].SortDirection = System.ComponentModel.ListSortDirection.Ascending;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public ObservableCollection<BaseLayerInfo> BaseLayers { get; }
        public IEnumerable<BaseLayerType> BaseLayerTypes { get; } = Enum.GetValues(typeof(BaseLayerType)).Cast<BaseLayerType>().ToList();
        public ArcMapView MapView { get; }

        private async void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Config.Instance.BaseLayers.Clear();

            foreach (var item in BaseLayers)
            {
                Config.Instance.BaseLayers.Add(item);
            }
            await MapView.LoadBasemapAsync();
            Config.Instance.Save();
            if ((sender as Button).Tag.Equals("1"))
            {
                Close();
            }
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            BaseLayerInfo layerInfo = new BaseLayerInfo(BaseLayerType.WebTiledLayer, "");
            BaseLayers.Add(layerInfo);
            grd.SelectedItem = layerInfo;
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e)
        {
            string path = FileSystemDialog.GetOpenFile(new FileFilterCollection()
                 .Add("JPEG图片", "jpg,jpeg")
                 .Add("PNG图片", "png")
                 .Add("BMP图片", "bmp")
                 .Add("TIFF图片", "tif,tiff")
                 .Add("Shapefile矢量图", "shp")
                 .Add("TilePackage切片包", "tpk")
                 .AddUnion());
            if (path == null)
            {
                return;
            }

            var layerInfo = (System.IO.Path.GetExtension(path)) switch
            {
                ".shp" => new BaseLayerInfo(BaseLayerType.ShapefileLayer, path),
                ".tpk" => new BaseLayerInfo(BaseLayerType.TpkLayer, path),
                _ => new BaseLayerInfo(BaseLayerType.RasterLayer, path),
            };
            BaseLayers.Add(layerInfo);
            grd.SelectedItem = layerInfo;
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (grd.SelectedItem != null)
            {
                BaseLayers.Remove(grd.SelectedItem as BaseLayerInfo);
            }
        }
    }

    public class BaseLayerTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (BaseLayerType)value switch
            {
                BaseLayerType.RasterLayer => "栅格图",
                BaseLayerType.TpkLayer => "切片包",
                BaseLayerType.ShapefileLayer => "Shapefile矢量图",
                BaseLayerType.WebTiledLayer => "网络瓦片图",
                _ => throw new NotSupportedException()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}