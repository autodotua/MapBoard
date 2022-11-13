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
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using EsriLayerCollection = Esri.ArcGISRuntime.Mapping.LayerCollection;
using System.Diagnostics;
using System.ComponentModel;
using AutoMapper;

namespace MapBoard.UI.Dialog
{
    public partial class ExportLayerDialog : CommonDialog
    {
        public IMapLayerInfo oldLayer = null;

        public ExportLayerDialog(MapLayerCollection layers, IMapLayerInfo layer, EsriLayerCollection esriLayers)
        {
            Fields.CollectionChanged += (s, e) => this.Notify(nameof(CanAddCreateTimeField), nameof(CanAddModifiedTimeField));
            InitializeComponent();
            Debug.Assert(layers != null);
            oldLayer = layer;
            LayerName = layer.Name;

            foreach (var field in layer.Fields)
            {
                var exportingFiled = new ExportingFieldInfo(field.Name, field.DisplayName, field.Type)
                {
                    OldField = field,
                    Enable = true
                };
                Fields.Add(exportingFiled);
            }
            Layers = layers;
            EsriLayers = esriLayers;
        }

        public bool CanAddCreateTimeField => !Fields.Any(p => p.Name == Parameters.CreateTimeFieldName);
        public bool CanAddModifiedTimeField => !Fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName);
        public EsriLayerCollection EsriLayers { get; }
        public ObservableCollection<ExportingFieldInfo> Fields { get; } = new ObservableCollection<ExportingFieldInfo>();

        public string LayerName { get; set; }

        public MapLayerCollection Layers { get; }
        public string Message { get; set; }

        private void AddFieldButton_Click(object sender, RoutedEventArgs e)
        {
            Fields.Add(new ExportingFieldInfo() { Enable = true });
        }

        private void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Column.DisplayIndex == 2)
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
                if (field.DisplayName.Length * field.Name.Length == 0
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

        private void CommonDialog_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            IsEnabled = false;

            try
            {
                await LayerUtility.ExportLayerAsync(oldLayer, Layers, LayerName, Fields);
                Hide();
            }
            catch (Exception ex)
            {
                App.Log.Error("导出图层失败", ex);
                Message = "导出图层失败：" + ex.Message;
            }

            IsEnabled = true;
        }

        public static ExportingFieldInfo CreateTimeField => new ExportingFieldInfo(Parameters.CreateTimeFieldName, "创建时间", FieldInfoType.Time);
        public static ExportingFieldInfo ModifiedTimeField => new ExportingFieldInfo(Parameters.ModifiedTimeFieldName, "修改时间", FieldInfoType.Time);
        private void CreateTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Fields.Any(p => p.Name == Parameters.CreateTimeFieldName))
            {
                Fields.Add(CreateTimeField);
            }
        }

        private void ModifiedTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName))
            {
                Fields.Add(ModifiedTimeField);
            }
        }
    }
}