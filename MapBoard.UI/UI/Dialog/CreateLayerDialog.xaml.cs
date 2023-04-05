using Esri.ArcGISRuntime.Geometry;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EsriLayerCollection = Esri.ArcGISRuntime.Mapping.LayerCollection;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 创建本地矢量图层对话框
    /// </summary>
    public partial class CreateLayerDialog : AddLayerDialogBase
    {
        /// <summary>
        /// 正在编辑的已有图层
        /// </summary>
        public MapLayerInfo editLayer = null;

        /// <summary>
        /// 图层类型
        /// </summary>
        private readonly string layerType;

        private CreateLayerDialog(MapLayerCollection layers, string layerType, MapLayerInfo layer, EsriLayerCollection esriLayers):base(layers)
        {
            this.layerType = layerType;
            Fields.CollectionChanged += (s, e) => this.Notify(nameof(CanAddCreateTimeField), nameof(CanAddModifiedTimeField));
            InitializeComponent();
            if (layer != null)
            {
                Title = layer switch
                {
                    ShapefileMapLayerInfo or TempMapLayerInfo => "编辑图层属性",
                    _ => throw new ArgumentException("图层类型不正确",nameof(layer))
                };
                if (layer is TempMapLayerInfo)
                {
                    Message = "设置后，将删除所有图形";
                }

                editLayer = layer;
                LayerName = layer.Name;
                grdType.Children.Cast<RadioButton>().ForEach(p => p.IsChecked = false);
                switch (layer.GeometryType)
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
                if (layer is not ICanChangeGeometryType)
                {
                    grdType.IsEnabled = false;
                }
                if (layer is not ICanChangeField)
                {
                    dg.Columns[0].IsReadOnly = true;
                    dg.Columns[2].IsReadOnly = true;
                    dg.CanUserAddRows = false;
                    dg.CanUserDeleteRows = false;
                    txtName.IsReadOnly = true;
                    stkAddField.Visibility = Visibility.Collapsed;
                }

                foreach (var field in layer.Fields)
                {
                    Fields.Add(field.Clone() as FieldInfo);
                }
            }
            else
            {
                LayerName = "新图层 - " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            }
            EsriLayers = esriLayers;
        }

        /// <summary>
        /// 能否增加创建时间字段
        /// </summary>
        public bool CanAddCreateTimeField => !Fields.Any(p => p.Name == Parameters.CreateTimeFieldName);
        
        /// <summary>
        /// 能否增加修改时间字段
        /// </summary>
        public bool CanAddModifiedTimeField => !Fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName);
        
        /// <summary>
        /// ArcGIS的图层
        /// </summary>
        public EsriLayerCollection EsriLayers { get; }

        /// <summary>
        /// 字段
        /// </summary>
        public ObservableCollection<FieldInfo> Fields { get; } = new ObservableCollection<FieldInfo>();

        /// <summary>
        /// 打开用于创建新图层的对话框
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="layers"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Task OpenCreateDialog<T>(MapLayerCollection layers) where T : MapLayerInfo
        {
            _ = layers ?? throw new ArgumentNullException(nameof(layers));
            return new CreateLayerDialog(layers, GetLayerType<T>(), null, null).ShowAsync();
        }

        /// <summary>
        /// 打开用于编辑已有图层的对话框
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="layers"></param>
        /// <param name="esriLayers"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Task OpenEditDialog<T>(MapLayerCollection layers, EsriLayerCollection esriLayers, T layer) where T : MapLayerInfo
        {
            _ = layers ?? throw new ArgumentNullException(nameof(layers));
            _ = layer ?? throw new ArgumentNullException(nameof(layer));
            return new CreateLayerDialog(layers, GetLayerType<T>(), layer, esriLayers).ShowAsync();
        }

        /// <summary>
        /// 根据类型<typeparamref name="T"/>获取<see cref="string"/>类型的图层类型名
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private static string GetLayerType<T>() where T : MapLayerInfo
        {
            if (typeof(T) == typeof(ShapefileMapLayerInfo))
            {
                return MapLayerInfo.Types.Shapefile;
            }
            else if (typeof(T) == typeof(TempMapLayerInfo))
            {
                return MapLayerInfo.Types.Temp;
            }
            throw new NotSupportedException("不支持的图层类型：" + typeof(T).Name);
        }

        /// <summary>
        /// 单击增加字段按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFieldButton_Click(object sender, RoutedEventArgs e)
        {
            Fields.Add(new FieldInfo());
        }

        /// <summary>
        /// 字段单元格编辑完成，判断目前的字段设置是否合规
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
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
        /// <exception cref="NotSupportedException"></exception>
        private async void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            IsEnabled = false;
            GeometryType type = 0 switch
            {
                0 when rbtnPoint.IsChecked == true => GeometryType.Point,
                0 when rbtnMultiPoint.IsChecked == true => GeometryType.Multipoint,
                0 when rbtnPolygon.IsChecked == true => GeometryType.Polygon,
                _ => GeometryType.Polyline,
            };
            if (editLayer != null)//编辑
            {
                switch (layerType)
                {
                    case MapLayerInfo.Types.Shapefile:
                        //(editLayer as ShapefileMapLayerInfo).ModifyFieldsAsync(Fields.ToArray(), EsriLayers);
                        editLayer.Fields = Fields.ToArray();
                        break;

                    case MapLayerInfo.Types.Temp:

                        (editLayer as TempMapLayerInfo).SetGeometryType(type);
                        editLayer.Fields = Fields.ToArray();
                        (editLayer as TempMapLayerInfo).ReloadAsync(Layers);
                        break;

                    default:
                        throw new NotSupportedException("不支持的图层类型：" + layerType);
                }

                Hide();
            }
            else//创建
            {
                var fields = Fields.Where(p => p.Name.Length > 0 && p.DisplayName.Length > 0)
                  .ToList();

                try
                {
                    switch (layerType)
                    {
                        case MapLayerInfo.Types.Shapefile:
                            await LayerUtility.CreateShapefileLayerAsync(type, Layers, name: LayerName, fields: fields);
                            break;

                        case MapLayerInfo.Types.Temp:
                            await LayerUtility.CreateTempLayerAsync(Layers, LayerName, type, fields);
                            break;

                        default:
                            throw new NotSupportedException("不支持的图层类型：" + layerType);
                    }

                    Hide();
                }
                catch (Exception ex)
                {
                    App.Log.Error("创建图层失败", ex);
                    Message = "创建图层失败：" + ex.Message;
                }
            }

            IsEnabled = true;
        }

        /// <summary>
        /// 单击生成创建时间字段的菜单项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Fields.Any(p => p.Name == Parameters.CreateTimeFieldName))
            {
                Fields.Add(FieldExtension.CreateTimeField);
            }
        }

        /// <summary>
        /// 单击生成修改时间字段的菜单项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModifiedTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName))
            {
                Fields.Add(FieldExtension.ModifiedTimeField);
            }
        }
    }
}