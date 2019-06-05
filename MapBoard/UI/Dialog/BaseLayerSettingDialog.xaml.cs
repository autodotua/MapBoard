using Esri.ArcGISRuntime.Data;
using FzLib.Control.Dialog;
using FzLib.Control.Extension;
using MapBoard.Common;
using MapBoard.Common.Dialog;
using MapBoard.Common.Resource;
using MapBoard.Main.Style;
using MapBoard.Main.UI.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static MapBoard.Common.BaseLayer.BaseLayerHelper;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class BaseLayerSettingDialog : DialogWindowBase
    {
        public Config Config => Config.Instance;
        public BaseLayerSettingDialog()
        {
            BaseLayers = new ObservableCollection<BaseLayerInfo>(
                Config.Instance.BaseLayers.Select(p => new BaseLayerInfo(p.Type, p.Path)));
            ResetIndex();
            BaseLayers.CollectionChanged += (p1, p2) => ResetIndex();
            InitializeComponent();
            new DataGridHelper<BaseLayerInfo>(grd).EnableDragAndDropItem();
           
        }

        private void ResetIndex()
        {
            for (int i = 0; i < BaseLayers.Count; i++)
            {
                BaseLayers[i].Index = i+1 ;
            }
            //grd.Columns[0].SortMemberPath = "Index";
            //grd.Columns[0].SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }



        public ObservableCollection<BaseLayerInfo> BaseLayers { get; }
        public string[] BaseLayerTypes { get; } = { WebTiledLayerDescription, RasterLayerDescription, ShapefileLayerDescription, TpkLayerDescription };

        
        private async void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Config.Instance.BaseLayers.Clear();

            foreach (var item in BaseLayers)
            {
                Config.Instance.BaseLayers.Add((item.Type, item.Path));
            }
            await ArcMapView.Instance.LoadBasemap();
            StyleCollection.ResetStyles();
            if ((sender as Button).Tag.Equals("1"))
            {
                Close();
            }
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            BaseLayerInfo layerInfo = new BaseLayerInfo(WebTiledLayer, "");
            BaseLayers.Add(layerInfo);
            grd.SelectedItem = layerInfo;
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e)
        {
            string path = FileSystemDialog.GetOpenFile(new List<(string, string)>()
                {
                    ("支持的格式", "jpg,jpeg,png,bmp,tif,tiff,shp,tpk"),
                    ("JPEG图片", "jpg,jpeg"),
                    ("PNG图片","png") ,
                    ("BMP图片","bmp") ,
                    ("TIFF图片","tif,tiff") ,
                    ("Shapefile矢量图","shp") ,
                    ("TilePackage切片包","tpk") ,
                }, true);
            BaseLayerInfo layerInfo;
            switch (System.IO.Path.GetExtension(path))
            {
                case ".shp":
                     layerInfo = new BaseLayerInfo(ShapefileLayer, path);
                    break;
                case ".tpk":
                     layerInfo = new BaseLayerInfo(TpkLayer, path);
                    break;
                default:
                     layerInfo = new BaseLayerInfo(RasterLayer, path);
                    break;
            }
            BaseLayers.Add(layerInfo);
            grd.SelectedItem = layerInfo;
        }
        public class BaseLayerInfo : FzLib.Extension.ExtendedINotifyPropertyChanged
        {
            private int index;

            public BaseLayerInfo(string type, string path)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Path = path ?? throw new ArgumentNullException(nameof(path));
            }

            public string Type { get; set; }
            public string TypeDescription
            {
                get
                {
                    switch (Type)
                    {
                        case WebTiledLayer:
                            return WebTiledLayerDescription;
                        case RasterLayer:
                            return RasterLayerDescription;
                        case ShapefileLayer:
                            return ShapefileLayerDescription;
                        case TpkLayer:
                            return TpkLayerDescription;
                        default:
                            throw new Exception("未知类型");

                    }
                }
                set
                {
                    switch (value)
                    {
                        case WebTiledLayerDescription:
                            Type = WebTiledLayer;
                            break;
                        case RasterLayerDescription:
                            Type = RasterLayer;
                            break;
                        case ShapefileLayerDescription:
                            Type = ShapefileLayer;
                            break;
                        case TpkLayerDescription:
                            Type = TpkLayer;
                            break;
                        default:
                            throw new Exception("未知类型");
                    }
                }
            }
            public string Path { get; set; }
            public int Index
            {
                get => index;
                set => SetValueAndNotify(ref index, value, nameof(Index));
            }
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if(grd.SelectedItem!=null)
            {
                BaseLayers.Remove(grd.SelectedItem as BaseLayerInfo);
            }
        }
    }

}
