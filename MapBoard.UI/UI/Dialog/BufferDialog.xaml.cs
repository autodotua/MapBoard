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
        public MapLayerCollection Layers { get; }
        public double Distance { get; set; } = 1000;
        public bool Union { get; set; }
        private bool toNewLayer = true;
        public bool ToNewLayer
        {
            get => toNewLayer;
            set => this.SetValueAndNotify(ref toNewLayer, value, nameof(ToNewLayer));
        }

        public IEditableLayerInfo TargetLayer { get; set; }
        public List<IEditableLayerInfo> PolygonLayers { get; }
        private string message;
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }


        public BufferDialog(MapLayerCollection layers)
        {
            Layers = layers;
            PolygonLayers = Layers
                .OfType<IEditableLayerInfo>()
                .Where(p => p .GeometryType is Esri.ArcGISRuntime.Geometry.GeometryType.Polygon)
                .ToList();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            if(Distance<=0)
            {
                Message = "距离超出范围";
                args.Cancel = true;
            }
            if(ToNewLayer==false && TargetLayer==null)
            {
                Message = "未指定目标图层";
                args.Cancel = true;
            }
        }
    }
}