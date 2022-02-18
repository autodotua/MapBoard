using Esri.ArcGISRuntime.Data;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class BufferDialog : CommonDialog
    {
        public BufferDialog(MapLayerCollection layers)
        {
            Layers = layers;
            PolygonLayers = Layers
                .EditableLayers
                .Where(p => p.GeometryType is Esri.ArcGISRuntime.Geometry.GeometryType.Polygon)
                .Cast<IEditableLayerInfo>()
                .ToList();
            InitializeComponent();
        }

        public double Distance { get; set; } = 1000;
        public MapLayerCollection Layers { get; }
        public string Message { get; set; }
        public List<IEditableLayerInfo> PolygonLayers { get; }
        public IEditableLayerInfo TargetLayer { get; set; }
        public bool ToNewLayer { get; set; } = true;
        public bool Union { get; set; }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            if (Distance <= 0)
            {
                Message = "距离超出范围";
                args.Cancel = true;
            }
            if (ToNewLayer == false && TargetLayer == null)
            {
                Message = "未指定目标图层";
                args.Cancel = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}