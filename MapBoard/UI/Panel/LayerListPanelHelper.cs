using System;
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
            List<(string header, Func<MapLayerInfo, Task> action, bool visiable)?> menus = null;
            MapLayerInfo layer = MapView.Layers.Selected;
            MapLayerInfo[] layers = list.SelectedItems.Cast<MapLayerInfo>().ToArray();
            if (list.SelectedItems.Count == 1)
            {
                if (layer != null && layer.NumberOfFeatures > 0)
                {
                    AddToMenu(menu, "缩放到图层", () => ZoomToLayerAsync(layer));
                }
                AddToMenu(menu, "属性表", () => ShowAttributeTableAsync(layer));
                AddToMenu(menu, "复制", () => CopyFeaturesAsync(layer));
                AddToMenu(menu, "删除", () => DeleteSelectedLayersAsync());
                AddToMenu(menu, "新建副本", () => CreateCopyAsync(layer));
                menu.Items.Add(new Separator());
                if (layer.GeometryType == GeometryType.Polyline
                    || layer.GeometryType == GeometryType.Point
                    || layer.GeometryType == GeometryType.Multipoint)
                {
                    AddToMenu(menu, "建立缓冲区", () => BufferAsync(layer));
                }
                AddToMenu(menu, "坐标转换", () => CoordinateTransformateAsync(layer));
                AddToMenu(menu, "设置时间范围", () => SetTimeExtentAsync(layer));
                AddToMenu(menu, "字段赋值", () => CopyAttributesAsync(layer));
                menu.Items.Add(new Separator());

                var menuImport = new MenuItem() { Header = "导入" };
                menu.Items.Add(menuImport);
                if (layer.GeometryType == GeometryType.Polyline
                    || layer.GeometryType == GeometryType.Point
                    || layer.GeometryType == GeometryType.Multipoint)
                {
                    AddToMenu(menuImport, "GPX轨迹文件", () => IOUtility.ImportFeatureAsync(layer, MapView, ImportLayerType.Gpx));
                }
                AddToMenu(menuImport, "CSV表格", () => IOUtility.ImportFeatureAsync(layer, MapView, ImportLayerType.Csv));

                var menuExport = new MenuItem() { Header = "导出" };
                menu.Items.Add(menuExport);

                AddToMenu(menuExport, "图层包", () => IOUtility.ExportLayerAsync(layer, MapView.Layers, ExportLayerType.LayerPackge));
                AddToMenu(menuExport, "GPS工具箱图层包", () => IOUtility.ExportLayerAsync(layer, MapView.Layers, ExportLayerType.GISToolBoxZip));
                AddToMenu(menuExport, "KML打包文件", () => IOUtility.ExportLayerAsync(layer, MapView.Layers, ExportLayerType.KML));
            }
            else
            {
                if (layers.Select(p => p.GeometryType).Distinct().Count() == 1)
                {
                    AddToMenu(menu, "合并", () => LayerUtility.UnionAsync(layers, MapView.Layers));
                }
                AddToMenu(menu, "删除", () => DeleteSelectedLayersAsync());
            }
            if (menu.Items.Count > 0)
            {
                menu.IsOpen = true;
            }
        }

        private void AddToMenu(ItemsControl menu, string header, Func<Task> func)
        {
            MenuItem item = new MenuItem() { Header = header };
            item.Click += async (p1, p2) =>
            {
                try
                {
                    await DoAsync(func);
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex);
                }
            };
            menu.Items.Add(item);
        }

        private async Task CopyFeaturesAsync(MapLayerInfo layer)
        {
            SelectLayerDialog dialog = new SelectLayerDialog(MapView.Layers);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.CopyAllFeaturesAsync(layer, dialog.SelectedLayer);
            }
        }

        private async Task EditFieldDisplayAsync(MapLayerInfo layer)
        {
            MapView.Selection.ClearSelection();
            CreateLayerDialog dialog = new CreateLayerDialog(MapView.Layers, layer);
            await dialog.ShowAsync();
        }

        private async Task CreateCopyAsync(MapLayerInfo layer)
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

        private async Task BufferAsync(MapLayerInfo layer)
        {
            var num = await CommonDialog.ShowDoubleInputDialogAsync("请输入缓冲区距离（米）");
            if (num.HasValue)
            {
                await LayerUtility.BufferAsync(layer, MapView.Layers, num.Value);
            }
        }

        private async Task ZoomToLayerAsync(MapLayerInfo layer)
        {
            try
            {
                await MapView.ZoomToGeometryAsync(await layer.QueryExtentAsync(new QueryParameters()));
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "操作失败，可能是不构成有面积的图形");
            }
        }

        private async Task CoordinateTransformateAsync(MapLayerInfo layer)
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

        private async Task SetTimeExtentAsync(MapLayerInfo layer)
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
            foreach (MapLayerInfo layer in list.SelectedItems.Cast<MapLayerInfo>().ToArray())
            {
                await layer.DeleteLayerAsync(MapView.Layers, true);
            }
        }

        private async Task CopyAttributesAsync(MapLayerInfo layer)
        {
            var dialog = new CopyAttributesDialog(layer);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await FeatureUtility.CopyAttributesAsync(layer, dialog.FieldSource, dialog.FieldTarget, dialog.DateFormat);
            }
        }

        private async Task ShowAttributeTableAsync(MapLayerInfo layer)
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
            while (obj != null && !(obj.DataContext is MapLayerInfo))
            {
                obj = obj.Parent as FrameworkElement;
            }
            var layer = obj.DataContext as MapLayerInfo;
            if (layer != null)
            {
                list.SelectedItem = layer;
            }
        }
    }
}