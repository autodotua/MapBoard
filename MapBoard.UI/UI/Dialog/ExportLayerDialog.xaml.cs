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
using EsriLayerCollection = Esri.ArcGISRuntime.Mapping.LayerCollection;
using System.Diagnostics;
using System.ComponentModel;
using AutoMapper;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 导出到新图层对话框
    /// </summary>
    public partial class ExportLayerDialog : AddLayerDialogBase
    {
        /// <summary>
        /// 旧图层
        /// </summary>
        public IMapLayerInfo oldLayer = null;

        public ExportLayerDialog(MapLayerCollection layers, IMapLayerInfo layer, EsriLayerCollection esriLayers):base(layers)
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
            EsriLayers = esriLayers;
        }

        /// <summary>
        /// 是否能增加创建时间按钮
        /// </summary>
        public bool CanAddCreateTimeField => !Fields.Any(p => p.Name == Parameters.CreateTimeFieldName);
       
        /// <summary>
        /// 是否能增加修改时间按钮
        /// </summary>
        public bool CanAddModifiedTimeField => !Fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName);
      
        /// <summary>
        /// 对应的ArcGIS图层
        /// </summary>
        public EsriLayerCollection EsriLayers { get; }

        /// <summary>
        /// 字段
        /// </summary>
        public ObservableCollection<ExportingFieldInfo> Fields { get; } = new ObservableCollection<ExportingFieldInfo>();

        /// <summary>
        /// 单击新增字段按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFieldButton_Click(object sender, RoutedEventArgs e)
        {
            Fields.Add(new ExportingFieldInfo() { Enable = true });
        }

        /// <summary>
        /// 单元格编辑结束，检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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


        /// <summary>
        /// 单击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
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

        /// <summary>
        /// 创建时间字段
        /// </summary>
        public static ExportingFieldInfo CreateTimeField => new ExportingFieldInfo(Parameters.CreateTimeFieldName, "创建时间", FieldInfoType.Time);
       
        /// <summary>
        /// 修改时间字段
        /// </summary>
        public static ExportingFieldInfo ModifiedTimeField => new ExportingFieldInfo(Parameters.ModifiedTimeFieldName, "修改时间", FieldInfoType.Time);
      
        /// <summary>
        /// 单击创建时间菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Fields.Any(p => p.Name == Parameters.CreateTimeFieldName))
            {
                Fields.Add(CreateTimeField);
            }
        }

        /// <summary>
        /// 单击修改时间菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModifiedTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName))
            {
                Fields.Add(ModifiedTimeField);
            }
        }
    }
}