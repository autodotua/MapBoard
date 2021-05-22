using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Common.Dialog;
using MapBoard.Main.Model;
using MapBoard.Main.Model.Extension;
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
        public bool editMode = false;
        public LayerInfo editLayer = null;

        public CreateLayerDialog(LayerInfo layer = null)
        {
            StaticFields = FieldInfo.DefaultFields;
            InitializeComponent();
            if (layer != null)
            {
                editMode = true;
                editLayer = layer;
                LayerName = layer.Name;
                grdType.Children.Cast<RadioButton>().ForEach(p => p.IsChecked = false);
                switch (layer.Table.GeometryType)
                {
                    case GeometryType.Point:
                        rbtnPoint.IsChecked = true;
                        break;

                    case GeometryType.Polyline:
                        rbtnPolyline.IsChecked = true;
                        break;

                    case GeometryType.Polygon:
                        rbtnPolygon.IsChecked = true;
                        break;

                    case GeometryType.Multipoint:
                        rbtnMultiPoint.IsChecked = true;
                        break;

                    default:
                        throw new NotSupportedException();
                }
                grdType.IsEnabled = false;

                dg.Columns[0].IsReadOnly = true;
                dg.Columns[2].IsReadOnly = true;
                dg.CanUserAddRows = false;
                dg.CanUserDeleteRows = false;

                foreach (var field in layer.Fields)
                {
                    Fields.Add(field.Clone() as FieldInfo);
                }
            }
            else
            {
                LayerName = "新图层 - " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
        }

        public ObservableCollection<FieldInfo> Fields { get; } = new ObservableCollection<FieldInfo>();
        public FieldInfo[] StaticFields { get; }
        private string layerName;

        public string LayerName
        {
            get => layerName;
            set => this.SetValueAndNotify(ref layerName, value, nameof(LayerName));
        }

        private void CommonDialog_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Column.DisplayIndex == 0)
                {
                    var field = e.Row.Item as FieldInfo;
                    if (string.IsNullOrEmpty(field.DisplayName))
                    {
                        field.DisplayName = field.Name;
                    }
                }
            }
            HashSet<string> names = new HashSet<string>();
            HashSet<string> displayNames = new HashSet<string>();
            foreach (var field in Fields)
            {
                if (field.Name == Resource.ClassFieldName
                    || field.Name == Resource.LabelFieldName
                    || field.Name == Resource.DateFieldName
                    || field.DisplayName.Length * field.Name.Length == 0
                    && field.Name.Length + field.DisplayName.Length > 0
                    || !displayNames.Add(field.DisplayName)
                    || !names.Add(field.Name))
                {
                    IsPrimaryButtonEnabled = false;
                    return;
                }
            }
            IsPrimaryButtonEnabled = true;
        }

        private async void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            IsPrimaryButtonEnabled = false;
            IsEnabled = false;
            CloseButtonText = null;

            if (editMode)
            {
                editLayer.Fields = Fields.ToArray();
            }
            else
            {
                var fields = Fields.Where(p => p.Name.Length > 0 && p.DisplayName.Length > 0)
                  .ToList();
                GeometryType type;
                if (rbtnPoint.IsChecked == true)
                {
                    type = GeometryType.Point;
                }
                if (rbtnMultiPoint.IsChecked == true)
                {
                    type = GeometryType.Multipoint;
                }
                else if (rbtnPolygon.IsChecked == true)
                {
                    type = GeometryType.Polygon;
                }
                else
                {
                    type = GeometryType.Polyline;
                }
                try
                {
                    await LayerUtility.CreateLayerAsync(type, name: LayerName, fields: fields);
                }
                catch (Exception ex)
                {
                    Hide();
                    await ShowErrorDialogAsync(ex, "创建图层失败");
                    return;
                }
            }
            Hide();
        }
    }
}