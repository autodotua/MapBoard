using Esri.ArcGISRuntime.Geometry;
using FzLib;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static MapBoard.Util.CoordinateTransformation;
using MapBoard.Mapping.Model;
using Esri.ArcGISRuntime.Ogc;
using MapBoard.UI.Converter;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AddWmsLayerDialog : CommonDialog
    {
        public AddWmsLayerDialog(BaseLayerType layerType)
        {
            InitializeComponent();

            LayerName = "图层";
            LayerType = layerType;
        }

        public bool AutoPopulateAll { get; set; }
        public string LayerName { get; set; }
        public MapLayerCollection Layers { get; }
        public BaseLayerType LayerType { get; }
        public string Message { get; set; }
        public string TypeName => BaseLayerTypeConverter.GetName(LayerType);
        public string Url { get; set; }

        public string WmsLayerName { get; set; }

        public ObservableCollection<WmsLayerInfo> WmsLayers { get; set; }
        public ObservableCollection<WmtsLayerInfo> WmtsLayers { get; set; }
        private void CommonDialog_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            if (Url != null && WmsLayerName != null)
            {
            }
            else
            {
                args.Cancel = true;
                Message = "请填写图层的完整信息";
            }
        }

        private async void QueryLayersButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            try
            {
                switch(LayerType)
                {
                    case BaseLayerType.WmsLayer:
                await QueryWmsLayers();
                        break;
                    case BaseLayerType.WmtsLayer:
                await QueryWmtsLayers();
                        break;
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("查询图层失败", ex);
                Message = "查询失败：" + ex.Message;
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private async Task QueryWmsLayers()
        {
            WmsService s = new WmsService(new Uri(Url));
            await s.LoadAsync();
            var layers = s.ServiceInfo.LayerInfos;
            WmsLayers = new ObservableCollection<WmsLayerInfo>();
            AddLayer(layers);
            void AddLayer(IReadOnlyList<WmsLayerInfo> layers)
            {
                foreach (var layer in layers)
                {
                    if (layer.LayerInfos.Count == 0)
                    {
                        WmsLayers.Add(layer);
                    }
                    else
                    {
                        AddLayer(layer.LayerInfos);
                    }
                }
            }

            cbbLayers.ItemsSource = WmsLayers;
            cbbLayers.DisplayMemberPath = "Name";
            if (WmsLayers.Count > 0)
            {
                cbbLayers.SelectedIndex = 0;
            }
        }

        private async Task QueryWmtsLayers()
        {
            WmtsService s = new WmtsService(new Uri(Url));
            await s.LoadAsync();
            var layers = s.ServiceInfo.LayerInfos;
            WmtsLayers = new ObservableCollection<WmtsLayerInfo>();
            foreach (var layer in layers)
            {
                WmtsLayers.Add(layer);
            }
            cbbLayers.ItemsSource = WmtsLayers;
            cbbLayers.DisplayMemberPath = "Id";
            if (WmtsLayers.Count > 0)
            {
                cbbLayers.SelectedIndex = 0;
            }
        }
    }
}