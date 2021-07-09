using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.WPF.Dialog;
using MapBoard.UI.Dialog;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MapBoard.Mapping;
using ModernWpf.Controls;
using ListView = System.Windows.Controls.ListView;

using System.Collections;

using System.Windows.Controls.Primitives;

using System.Windows.Media;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using WinRT;

namespace MapBoard.UI
{
    public class LayerListPanelHelper
    {
        private readonly ListView list;

        public MainWindow MainWindow { get; }
        public MainMapView MapView { get; }

        public LayerListPanelHelper(ListView list, MainWindow win, MainMapView mapView)
        {
            this.list = list;
            MainWindow = win;
            MapView = mapView;
        }

        public void ShowContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            MapLayerInfo layer = MapView.Layers.Selected;
            MapLayerInfo[] layers = list.SelectedItems.Cast<MapLayerInfo>().ToArray();
            if (list.SelectedItems.Count == 1)
            {
                if (layer != null && layer.NumberOfFeatures > 0)
                {
                    AddToMenu(menu, "缩放到图层", () => ZoomToLayerAsync(layer));
                }
                AddToMenu(menu, "属性表", () => ShowAttributeTableAsync(layer));
                AddToMenu(menu, "复制图形到", () => CopyFeaturesAsync(layer));
                AddToMenu(menu, "删除", () => DeleteLayersAsync(layers));
                AddToMenu(menu, "建立副本", () => CreateCopyAsync(layer));
                AddToMenu(menu, "编辑字段别名", () => EditFieldDisplayAsync(layer));
                menu.Items.Add(new Separator());
                AddToMenu(menu, "查询要素", () => QueryAsync(layer));

                if (layer.GeometryType == GeometryType.Polyline
                    || layer.GeometryType == GeometryType.Point
                    || layer.GeometryType == GeometryType.Multipoint)
                {
                    AddToMenu(menu, "建立缓冲区", () => BufferAsync(layer));
                }
                AddToMenu(menu, "坐标转换", () => CoordinateTransformateAsync(layer));
                AddToMenu(menu, "设置时间范围", () => SetTimeExtentAsync(layer));
                AddToMenu(menu, "字段赋值", () => CopyAttributesAsync(layer));
                AddToMenu(menu, "操作历史记录", () => OpenHistoryDialog(layer));
                menu.Items.Add(new Separator());

                var menuImport = new MenuItem() { Header = "导入" };
                menu.Items.Add(menuImport);
                if (layer.GeometryType == GeometryType.Polyline
                    || layer.GeometryType == GeometryType.Point
                    || layer.GeometryType == GeometryType.Multipoint)
                {
                    AddToMenu(menuImport, "GPX轨迹文件",
                        () => IOUtility.GetImportFeaturePath(ImportLayerType.Gpx),
                        p => IOUtility.ImportFeatureAsync(MainWindow, p, layer, MapView, ImportLayerType.Gpx),
                        "正在导入GPX轨迹文件");
                }

                AddToMenu(menuImport, "CSV文件",
                    () => IOUtility.GetImportFeaturePath(ImportLayerType.Csv),
                    p => IOUtility.ImportFeatureAsync(MainWindow, p, layer, MapView, ImportLayerType.Csv),
                    "正在导入CSV文件");

                var menuExport = new MenuItem() { Header = "导出" };
                menu.Items.Add(menuExport);

                AddToMenu(menuExport, "图层包",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.LayerPackge),
                    p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.LayerPackge),
                    "正在导出图层包");
                if (Config.Instance.CopyShpFileWhenExport)
                {
                    AddToMenu(menuExport, "图层包（重建）",
                        () => IOUtility.GetExportLayerPath(layer, ExportLayerType.LayerPackgeRebuild),
                        p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.LayerPackgeRebuild),
                        "正在导出图层包");
                }
                AddToMenu(menuExport, "GPS工具箱图层包",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.GISToolBoxZip),
                    p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.GISToolBoxZip),
                    "正在导出GPS工具箱图层包");
                AddToMenu(menuExport, "KML打包文件",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.KML),
                    p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.KML),
                    "正在导出KML打包文件");
                AddToMenu(menuExport, "GeoJSON文件",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.GeoJSON),
                    p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.GeoJSON),
                    "正在导出GeoJSON文件");
            }
            else
            {
                if (layers.Select(p => p.GeometryType).Distinct().Count() == 1)
                {
                    AddToMenu(menu, "合并", () => LayerUtility.UnionAsync(layers, MapView.Layers));
                }
                AddToMenu(menu, "删除", () => DeleteLayersAsync(layers));
                AddToMenu(menu, "坐标转换", () => CoordinateTransformateAsync(layers));
            }
            if (menu.Items.Count > 0)
            {
                menu.IsOpen = true;
            }
        }

        private async Task QueryAsync(MapLayerInfo layer)
        {
            await Task.Yield();
            var dialog = new QueryFeaturesDialog(MainWindow, MapView, layer);
            dialog.BringToFront();
        }

        private async Task OpenHistoryDialog(MapLayerInfo layer)
        {
            await Task.Yield();
            var dialog = FeatureHistoryDialog.Get(MainWindow, layer, MapView);
            dialog.BringToFront();
        }

        private void AddToMenu(ItemsControl menu, string header, Func<Task> func)
        {
            MenuItem item = new MenuItem() { Header = header };
            item.Click += async (p1, p2) =>
            {
                try
                {
                    await MainWindow.DoAsync(func, "正在处理");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex);
                }
            };
            menu.Items.Add(item);
        }

        private void AddToMenu(ItemsControl menu, string header, Func<string> getPath, Func<string, Task> func, string message)
        {
            MenuItem item = new MenuItem() { Header = header };
            item.Click += async (p1, p2) =>
            {
                try
                {
                    string path = getPath();
                    if (path != null)
                    {
                        await MainWindow.DoAsync(async () => await func(path), message);
                    }
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
            SelectLayerDialog dialog = new SelectLayerDialog(MapView.Layers, new[] { layer.GeometryType }, true);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.CopyAllFeaturesAsync(layer, dialog.SelectedLayer);
            }
        }

        private async Task EditFieldDisplayAsync(MapLayerInfo layer)
        {
            if (layer.Fields.Length == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("该图层没有自定义字段");
                return;
            }
            MapView.Selection.ClearSelection();
            CreateLayerDialog dialog = new CreateLayerDialog(MapView.Layers, layer);
            await dialog.ShowAsync();
        }

        private async Task CreateCopyAsync(MapLayerInfo layer)
        {
            var check = await CommonDialog.ShowCheckBoxDialogAsync("请选择副本类型",
                    new CheckDialogItem[]
                {
              new  CheckDialogItem("样式",null,false,true),
               new CheckDialogItem("字段",null){Tag=1 },
               new CheckDialogItem("所有图形",null){Tag=2}
                }, true);
            if (check != null)
            {
                bool includeFields = check.Any(p => 1.Equals(p.Tag));
                bool includeFeatures = check.Any(p => 2.Equals(p.Tag));
                await LayerUtility.CreatCopyAsync(layer, MapView.Layers, includeFeatures, includeFields);
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
                await CommonDialog.ShowErrorDialogAsync(ex, "缩放失败，可能是不构成有面积的图形");
            }
        }

        private async Task CoordinateTransformateAsync(MapLayerInfo layer)
        {
            CoordinateTransformationDialog dialog = new CoordinateTransformationDialog();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.Source != dialog.Target)
            {
                await MainWindow.DoAsync(async () =>
                {
                    await LayerUtility.CoordinateTransformateAsync(layer, dialog.Source, dialog.Target);
                }, "正在进行坐标转换");
            }
        }

        private async Task CoordinateTransformateAsync(IList<MapLayerInfo> layers)
        {
            CoordinateTransformationDialog dialog = new CoordinateTransformationDialog();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.Source != dialog.Target)
            {
                await MainWindow.DoAsync(async p =>
                {
                    int index = 0;
                    foreach (var layer in layers)
                    {
                        p.SetMessage($"正在转换图层{++index}/{layers.Count}：{layer.Name}");
                        await LayerUtility.CoordinateTransformateAsync(layer, dialog.Source, dialog.Target);
                    }
                }, "正在进行坐标转换");
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

        private async Task DeleteLayersAsync(IList<MapLayerInfo> layers)
        {
            if (list.SelectedItems.Count == 0)
            {
                SnakeBar.ShowError("没有选择任何样式");
                return;
            }
            foreach (MapLayerInfo layer in layers)
            {
                await layer.DeleteLayerAsync(MapView.Layers, true);
            }
        }

        private async Task CopyAttributesAsync(MapLayerInfo layer)
        {
            var dialog = new CopyAttributesDialog(layer);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ItemsOperationErrorCollection errors = null;

                switch (dialog.Type)
                {
                    case CopyAttributesType.Field:
                        errors = await AttributeUtility.CopyAttributesAsync(layer, dialog.SourceField, dialog.TargetField, dialog.DateFormat);
                        break;

                    case CopyAttributesType.Const:
                        errors = await AttributeUtility.SetAttributesAsync(layer, dialog.TargetField, dialog.Text, false, dialog.DateFormat);

                        break;

                    case CopyAttributesType.Custom:
                        errors = await AttributeUtility.SetAttributesAsync(layer, dialog.TargetField, dialog.Text, true, dialog.DateFormat);
                        break;

                    default:
                        break;
                }
                await ItemsOperaionErrorsDialog.TryShowErrorsAsync("部分属性复制失败", errors);
            }
        }

        private async Task ShowAttributeTableAsync(MapLayerInfo layer)
        {
            var dialog = AttributeTableDialog.Get(MainWindow, layer, MapView);
            try
            {
                await MainWindow.DoAsync(dialog.LoadAsync, "正在加载属性表");
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "加载属性失败");
                return;
            }
            dialog.BringToFront();
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