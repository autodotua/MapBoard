﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.WPF.Dialog;
using MapBoard.Common;

using MapBoard.Main.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MapBoard.Main.UI.Map;
using ModernWpf.Controls;
using ListView = System.Windows.Controls.ListView;

namespace MapBoard.Main.UI.Panel
{
    public class LayerListPanelHelper
    {
        private readonly ListView list;

        public Func<Func<Task>, Task> DoAsync { get; }
        public ArcMapView MapView { get; }

        public LayerListPanelHelper(ListView list, Func<Func<Task>, Task> d, ArcMapView mapView)
        {
            this.list = list;
            DoAsync = d;
            MapView = mapView;
        }

        public void ShowContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            List<(string header, Func<LayerInfo, Task> action, bool visiable)?> menus = null;
            LayerInfo layer = MapView.Layers.Selected;
            LayerInfo[] layers = list.SelectedItems.Cast<LayerInfo>().ToArray();
            if (list.SelectedItems.Count == 1)
            {
                menus = new List<(string header, Func<LayerInfo, Task> action, bool visiable)?>()
               {
                    ("缩放到图层", ZoomToLayerAsync,layer.Table!=null&&layer.Table.NumberOfFeatures > 0),
                    ("属性表", ShowAttributeTableAsync,true),
                    ("复制", CopyFeaturesAsync,true),
                    ("删除",async l=>await DeleteSelectedLayersAsync(),true),
                    ("修改字段显示名", EditFieldDisplayAsync,true),
                    null,
                    ("建立缓冲区",BufferAsync,layer.Table.GeometryType==GeometryType.Polyline || layer.Table.GeometryType==GeometryType.Point|| layer.Table.GeometryType==GeometryType.Multipoint),
                    ("新建副本", CreateCopyAsync,true),
                    ("坐标转换",CoordinateTransformateAsync,true),
                    ("设置时间范围",SetTimeExtentAsync,layer.Table.Fields.Any(p=>p.FieldType==FieldType.Date && p.Name==Resource.DateFieldName)),
                    ("字段赋值",CopyAttributesAsync,true),
                    null,
                    ("导入",async p=>await IOUtility.ImportFeatureAsync(p, MapView),true),
                    ("导出",  ExportSingleAsync,true),
               };
            }
            else
            {
                menus = new List<(string header, Func<LayerInfo, Task> action, bool visiable)?>()
               {
                    ("合并",async l=>await LayerUtility. UnionAsync(layers,MapView.Layers)
                    ,layers.Select(p=>p.Table.GeometryType).Distinct().Count()==1),
                    ("删除",async l=>await DeleteSelectedLayersAsync(),true),
                };
            }

            foreach (var m in menus)
            {
                if (m.HasValue)
                {
                    if (m.Value.visiable)
                    {
                        MenuItem item = new MenuItem() { Header = m.Value.header };
                        item.Click += async (p1, p2) =>
                        {
                            try
                            {
                                await DoAsync(() => m.Value.action(layer));
                            }
                            catch (Exception ex)
                            {
                                await CommonDialog.ShowErrorDialogAsync(ex);
                            }
                        };
                        menu.Items.Add(item);
                    }
                }
                else
                {
                    menu.Items.Add(new Separator());
                }
            }
            if (menu.Items.Count > 0)
            {
                menu.IsOpen = true;
            }
        }

        private async Task CopyFeaturesAsync(LayerInfo layer)
        {
            SelectLayerDialog dialog = new SelectLayerDialog(MapView.Layers);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.CopyAllFeaturesAsync(layer, dialog.SelectedLayer);
            }
        }

        private async Task EditFieldDisplayAsync(LayerInfo layer)
        {
            MapView.Selection.ClearSelection();
            CreateLayerDialog dialog = new CreateLayerDialog(MapView.Layers, layer);
            await dialog.ShowAsync();
        }

        private async Task CreateCopyAsync(LayerInfo layer)
        {
            int mode = 0;
            await CommonDialog.ShowSelectItemDialogAsync("请选择副本类型",
                new DialogItem[]
            {
              new  DialogItem("仅样式",null,()=>mode=1),
               new DialogItem("样式和所有图形",null,()=>mode=2)
            });
            if (mode > 0)
            {
                await LayerUtility.CreatCopyAsync(layer, MapView.Layers, mode == 2);
            }
        }

        private async Task BufferAsync(LayerInfo layer)
        {
            await LayerUtility.BufferAsync(layer, MapView.Layers);
        }

        private Task ExportSingleAsync(LayerInfo layer)
        {
            return IOUtility.ExportLayerAsync(layer, MapView.Layers);
        }

        private async Task ZoomToLayerAsync(LayerInfo layer)
        {
            try
            {
                await MapView.ZoomToGeometryAsync(await layer.Table.QueryExtentAsync(new QueryParameters()));
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "操作失败，可能是不构成有面积的图形");
            }
        }

        private async Task CoordinateTransformateAsync(LayerInfo layer)
        {
            CoordinateTransformationDialog dialog = new CoordinateTransformationDialog();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await DoAsync(async () =>
                {
                    string from = dialog.SelectedCoordinateSystem1;
                    string to = dialog.SelectedCoordinateSystem2;
                    await LayerUtility.CoordinateTransformateAsync(layer, from, to);
                });
            }
        }

        private async Task SetTimeExtentAsync(LayerInfo layer)
        {
            DateRangeDialog dialog = new DateRangeDialog(MapView.Layers.Selected);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.SetTimeExtentAsync(layer);
            }
        }

        private async Task DeleteSelectedLayersAsync()
        {
            if (list.SelectedItems.Count == 0)
            {
                SnakeBar.ShowError("没有选择任何样式");
                return;
            }
            foreach (LayerInfo layer in list.SelectedItems.Cast<LayerInfo>().ToArray())
            {
                await layer.DeleteLayerAsync(MapView.Layers, true);
            }
        }

        private async Task CopyAttributesAsync(LayerInfo layer)
        {
            var dialog = new CopyAttributesDialog(layer);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await FeatureUtility.CopyAttributesAsync(layer, dialog.FieldSource, dialog.FieldTarget, dialog.DateFormat);
            }
        }

        private async Task ShowAttributeTableAsync(LayerInfo layer)
        {
            var dialog = new AttributeTableDialog(layer, MapView);
            try
            {
                await DoAsync(dialog.LoadAsync);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "加载属性失败");
                return;
            }
            dialog.Show();
        }

        public void RightButtonClickToSelect(MouseEventArgs e)
        {
            return;
            if (list.SelectedItems.Count > 1)
            {
                return;
            }
            var obj = e.OriginalSource as FrameworkElement;
            while (obj != null && !(obj.DataContext is LayerInfo))
            {
                obj = obj.Parent as FrameworkElement;
            }
            var layer = obj.DataContext as LayerInfo;
            if (layer != null)
            {
                list.SelectedItem = layer;
            }
        }
    }
}