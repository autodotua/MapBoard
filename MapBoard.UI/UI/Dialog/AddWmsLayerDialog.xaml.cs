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
    /// 新增WMS或WMTS底图
    /// </summary>
    public partial class AddWmsLayerDialog : AddLayerDialogBase
    {
        public AddWmsLayerDialog(BaseLayerType layerType)
        {
            InitializeComponent();

            LayerName = "图层";
            LayerType = layerType;
        }

        /// <summary>
        /// 图层类型，WMS或是WMTS
        /// </summary>
        public BaseLayerType LayerType { get; }

        /// <summary>
        /// 图层类型名
        /// </summary>
        public string TypeName => BaseLayerTypeConverter.GetName(LayerType);

        /// <summary>
        /// WMS或WMTS服务的图层名
        /// </summary>
        public string WmsLayerName { get; set; }

        /// <summary>
        /// WMS服务中的图层
        /// </summary>
        public ObservableCollection<WmsLayerInfo> WmsLayers { get; set; }

        /// <summary>
        /// WMTS服务中的图层
        /// </summary>
        public ObservableCollection<WmtsLayerInfo> WmtsLayers { get; set; }
   
        /// <summary>
        /// 单击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            if (Url == null || WmsLayerName == null)
            {
                args.Cancel = true;
                Message = "请填写图层的完整信息";
            }
        }

        /// <summary>
        /// 单击查询服务图层按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 查询WMS图层
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 查询WMTS图层
        /// </summary>
        /// <returns></returns>
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