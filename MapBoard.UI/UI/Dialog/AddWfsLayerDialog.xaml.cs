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
    public partial class AddWfsLayerDialog : CommonDialog
    {
        public WfsMapLayerInfo editLayer = null;
        public bool editMode = false;

        public AddWfsLayerDialog(MapLayerCollection layers, WfsMapLayerInfo layer = null)
        {
            InitializeComponent();
            if (layer != null)
            {
                Title = "编辑WFS图层";
                editMode = true;
                editLayer = layer;
                LayerName = layer.Name;
                WfsLayerName = layer.LayerName;
                AutoPopulateAll = layer.AutoPopulateAll;
                Url = layer.Url;
            }
            else
            {
                LayerName = "新图层 - " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            Layers = layers;
        }

        public bool AutoPopulateAll { get; set; }
        public string LayerName { get; set; }
        public MapLayerCollection Layers { get; }
        public string Message { get; set; }
        public string Url { get; set; }
        public string WfsLayerName { get; set; }
        public IReadOnlyList<WfsLayerInfo> WfsLayers { get; set; }

        private void CommonDialog_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            IsEnabled = false;
            if (editMode)
            {
                editLayer.SetService(Url, WfsLayerName, AutoPopulateAll);
                try
                {
                    await editLayer.ReloadAsync(Layers);
                    Hide();
                }
                catch (Exception ex)
                {
                    App.Log.Error("架子啊Wfs失败", ex);
                    Message = "加载失败：" + ex.Message;
                }
            }
            else
            {
                try
                {
                    await LayerUtility.AddWfsLayerAsync(Layers, LayerName, Url, WfsLayerName, AutoPopulateAll);
                    Hide();
                }
                catch (Exception ex)
                {
                    Message = "创建图层失败：" + ex.Message;
                    return;
                }
            }
            IsEnabled = true;
        }

        private async void QueryLayersButton_Click(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            try
            {
                WfsService s = new WfsService(new Uri(Url));
                await s.LoadAsync();
                WfsLayers = s.ServiceInfo.LayerInfos;
                if (WfsLayers.Count > 0)
                {
                    cbbWfsLayers.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("查询Wfs图层失败", ex);
                Message = "查询失败：" + ex.Message;
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}