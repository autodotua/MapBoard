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

        private string message;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        private string layerName;

        public string LayerName
        {
            get => layerName;
            set => this.SetValueAndNotify(ref layerName, value, nameof(LayerName));
        }

        private bool autoPopulateAll;

        public bool AutoPopulateAll
        {
            get => autoPopulateAll;
            set => this.SetValueAndNotify(ref autoPopulateAll, value, nameof(AutoPopulateAll));
        }

        private string url;

        public string Url
        {
            get => url;
            set => this.SetValueAndNotify(ref url, value, nameof(Url));
        }

        private string wmsLayerName;

        public string WmsLayerName
        {
            get => wmsLayerName;
            set => this.SetValueAndNotify(ref wmsLayerName, value, nameof(WmsLayerName));
        }

        private ObservableCollection<WmsLayerInfo> wmtsLayers;

        public ObservableCollection<WmsLayerInfo> WmsLayers
        {
            get => wmtsLayers;
            set => this.SetValueAndNotify(ref wmtsLayers, value, nameof(WmsLayers));
        }

        public MapLayerCollection Layers { get; }

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
                WmsService s = new WmsService(new Uri(url));
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
                Message = "查询失败：" + ex.Message;
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}