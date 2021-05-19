using Esri.ArcGISRuntime.Geometry;
using MapBoard.Common;
using MapBoard.Common.Dialog;
using MapBoard.Main.Model;
using MapBoard.Main.Util;
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
using static MapBoard.Common.CoordinateTransformation;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CreateLayerDialog : CommonDialog
    {
        public CreateLayerDialog()
        {
            LayerName = "新图层 - " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            StaticFields = new FieldInfo[]
            {
                new FieldInfo("Label", "标签", FieldInfoType.Text),
                new FieldInfo("Date", "日期", FieldInfoType.Date),
                new FieldInfo("Class", "分类", FieldInfoType.Text),
            };
            InitializeComponent();
        }

        public ObservableCollection<FieldInfo> Fields { get; } = new ObservableCollection<FieldInfo>();
        public FieldInfo[] StaticFields { get; }
        public string LayerName { get; set; }

        private void CommonDialog_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            foreach (var field in Fields)
            {
                if (field.DisplayName.Length * field.Name.Length == 0
                    && field.Name.Length + field.DisplayName.Length > 0)
                {
                    IsPrimaryButtonEnabled = false;
                    return;
                }
            }
            IsPrimaryButtonEnabled = true;
        }

        private async void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            var fields = StaticFields
                .Concat(Fields.Where(p => p.Name.Length > 0 && p.DisplayName.Length > 0))
                .ToEsriFields()
                .ToList();
            GeometryType type;
            if (rbtnPoint.IsChecked == true)
            {
                type = GeometryType.Point;
            }
            else if (rbtnPolygon.IsChecked == true)
            {
                type = GeometryType.Polygon;
            }
            else
            {
                type = GeometryType.Polyline;
            }
            args.Cancel = true;
            IsPrimaryButtonEnabled = false;
            IsEnabled = false;
            CloseButtonText = null;
            await LayerUtility.CreateLayerAsync(type, name: LayerName, fields: fields);
            Hide();
        }
    }
}