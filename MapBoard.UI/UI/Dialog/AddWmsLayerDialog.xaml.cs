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

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AddWmsLayerDialog : CommonDialog
    {
        public AddWmsLayerDialog()
        {
            InitializeComponent();

            LayerName = "WMS图层";
        }

        public bool AutoPopulateAll { get; set; }
        public string LayerName { get; set; }
        public MapLayerCollection Layers { get; }
        public string Message { get; set; }
        public string Url { get; set; }

        public string WmsLayerName { get; set; }

        public ObservableCollection<WmsLayerInfo> WmsLayers { get; set; }

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

                if (WmsLayers.Count > 0)
                {
                    cbbLayers.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("查询Wms图层失败", ex);
                Message = "查询失败：" + ex.Message;
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}