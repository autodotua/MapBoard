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
using System.Data;
using Esri.ArcGISRuntime.Data;
using System.Globalization;
using MapBoard.UI.Model;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ImportTableDialog : CommonDialog
    {
        public MapLayerInfo editLayer = null;

        public ImportTableDialog(MapLayerCollection layers, DataTable table, string defaultName)
        {
            LayerName = defaultName;

            Layers = layers;
            Table = table;
            LoadFields();
            InitializeComponent();
        }

        private void LoadFields()
        {
            int index = 0;
            foreach (DataColumn column in Table.Columns)
            {
                ImportTableFieldInfo field = new ImportTableFieldInfo()
                {
                    ColumnName = column.ColumnName,
                    ColumnIndex = ++index
                };
                field.Field.Name = new string(column.ColumnName.Take(10).ToArray());
                field.Field.DisplayName = column.ColumnName;
                field.Field.Type = FieldInfoType.Text;
                Fields.Add(field);
            }
        }

        public ObservableCollection<ImportTableFieldInfo> Fields { get; } = new ObservableCollection<ImportTableFieldInfo>();
        public int LongitudeIndex { get; set; } = -1;
        public int LatitudeIndex { get; set; } = -1;
        private string layerName;

        public string LayerName
        {
            get => layerName;
            set => this.SetValueAndNotify(ref layerName, value, nameof(LayerName));
        }

        public MapLayerCollection Layers { get; }
        public DataTable Table { get; }
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        private string message;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        private void CommonDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Check();
        }

        private void dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Column.DisplayIndex == 0)
                {
                    var field = e.Row.Item as ImportTableFieldInfo;
                    if (string.IsNullOrEmpty(field.Field.DisplayName))
                    {
                        field.Field.DisplayName = field.Field.Name;
                    }
                }
            }
            Check();
        }

        private void Check()
        {
            HashSet<string> names = new HashSet<string>();
            HashSet<string> displayNames = new HashSet<string>();
            foreach (var field in Fields.Where(p => p.Import))
            {
                if (field.Field.Name == Parameters.ClassFieldName
                    || field.Field.Name == Parameters.LabelFieldName
                    || field.Field.Name == Parameters.DateFieldName
                    || field.Field.DisplayName.Length * field.Field.Name.Length == 0
                    || !displayNames.Add(field.Field.DisplayName)
                    || !names.Add(field.Field.Name))
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
            if (LongitudeIndex < 0 || LatitudeIndex < 0)
            {
                Message = "请先选择经度和纬度字段";
                return;
            }
            IsPrimaryButtonEnabled = false;
            IsEnabled = false;
            CloseButtonText = null;

            Hide();
            var fields = Fields.Where(p => p.Import)
                .Select(p => p.Field).ToList();
            try
            {
                var layer = await LayerUtility.CreateShapefileLayerAsync(GeometryType.Point, Layers, name: LayerName, fields: fields);
                int failedRowCount = 0;
                List<Feature> features = new List<Feature>();
                await Task.Run(() =>
                {
                    foreach (DataRow row in Table.Rows)
                    {
                        double x = 0, y = 0;
                        try
                        {
                            x = double.Parse(row[LongitudeIndex - 1].ToString());
                            y = double.Parse(row[LatitudeIndex - 1].ToString());

                            Feature feature = layer.CreateFeature();

                            feature.Geometry = new MapPoint(x, y);
                            for (int i = 0; i < Fields.Count; i++)
                            {
                                var field = Fields[i];
                                if (!field.Import)
                                {
                                    continue;
                                }
                                string strValue = row[i]?.ToString();
                                if (string.IsNullOrEmpty(strValue))
                                {
                                    continue;
                                }
                                object value = null;
                                value = field.Field.Type switch
                                {
                                    FieldInfoType.Integer => int.Parse(strValue),
                                    FieldInfoType.Float => double.Parse(strValue),
                                    FieldInfoType.Date => DateTime.ParseExact(strValue, DateFormat, CultureInfo.InvariantCulture),
                                    FieldInfoType.Text => strValue,
                                    FieldInfoType.Time => DateTime
                                        .ParseExact(strValue, DateFormat, CultureInfo.InvariantCulture)
                                        .ToString(Parameters.TimeFormat),
                                    _ => throw new NotSupportedException(),
                                };
                                feature.SetAttributeValue(field.Field.Name, value);
                            }
                            features.Add(feature);
                        }
                        catch (Exception ex)
                        {
                            App.Log.Error("保存属性失败", ex);
                            failedRowCount++;
                            continue;
                        }
                    }
                    layer.AddFeaturesAsync(features, FeaturesChangedSource.Import);
                });
                Hide();
                await ShowOkDialogAsync($"导入了{features.Count}条，失败了{failedRowCount}条");
            }
            catch (Exception ex)
            {
                App.Log.Error("创建图层失败", ex);
                Hide();
                await ShowErrorDialogAsync(ex, "创建图层失败");
                return;
            }
        }
    }
}