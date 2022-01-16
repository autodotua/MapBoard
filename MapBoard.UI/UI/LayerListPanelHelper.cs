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
using MapBoard.Mapping.Model;
using MapBoard.Model;
using System.Drawing;
using MapBoard.IO;
using System.IO;

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
            IMapLayerInfo layer = MapView.Layers.Selected;
            MapLayerInfo[] layers = list.SelectedItems.Cast<MapLayerInfo>().ToArray();
            if (list.SelectedItems.Count == 1)
            {
                menu.Items.Add(new MenuItem()
                {
                    IsEnabled = false,
                    Header = new TextBlock()
                    {
                        Text = MapLayerInfo.Types.GetDescription(layer.Type),
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = App.Current.FindResource("SystemControlForegroundBaseHighBrush") as System.Windows.Media.Brush,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 24)
                    }
                });
                if (!layer.IsLoaded)
                {
                    AddToMenu(menu, "删除", () => DeleteLayersAsync(layers));
                    AddToMenu<WfsMapLayerInfo>(menu, "设置图层", layer, SetWfsLayerAsync);
                    AddToMenu(menu, "重新加载", () => ReloadLayerAsync(layer));
                }
                else
                {
                    if (layer != null && layer.NumberOfFeatures > 0)
                    {
                        AddToMenu(menu, "缩放到图层", () => ZoomToLayerAsync(layer));
                    }
                    AddToMenu(menu, "属性表", () => ShowAttributeTableAsync(layer));
                    AddToMenu(menu, "复制图形到", () => CopyFeaturesAsync(layer));
                    AddToMenu(menu, "删除", () => DeleteLayersAsync(layers));
                    AddToMenu(menu, layer is IFileBasedLayer ? "建立副本" : "建立持久副本", () => CreateCopyAsync(layer));

                    AddToMenu<IServerBasedLayer>(menu, "下载全部图形", layer, PopulateAllAsync);
                    AddToMenu<ShapefileMapLayerInfo>(menu, "设置图层", layer, SetShapefileLayerAsync);
                    AddToMenu<WfsMapLayerInfo>(menu, "设置图层", layer, SetWfsLayerAsync);
                    AddToMenu<TempMapLayerInfo>(menu, "设置图层", layer, SetTempLayerAsync);
                    AddToMenu<TempMapLayerInfo>(menu, "重置", layer, ResetTempLayerAsync);

                    menu.Items.Add(new Separator());
                    AddToMenu(menu, "查询要素", () => QueryAsync(layer));

                    if (layer.GeometryType == GeometryType.Polyline
                        || layer.GeometryType == GeometryType.Point
                        || layer.GeometryType == GeometryType.Multipoint)
                    {
                        AddToMenu(menu, "建立缓冲区", () => BufferAsync(layer));
                    }
                    AddToMenu<IEditableLayerInfo>(menu, "坐标转换", layer, CoordinateTransformateAsync);
                    AddToMenu<IEditableLayerInfo>(menu, "字段赋值", layer, CopyAttributesAsync);
                    AddToMenu<IEditableLayerInfo>(menu, "操作历史记录", layer, OpenHistoryDialog);
                    AddToMenu<IHasDefaultFields>(menu, "设置时间范围", layer, SetTimeExtentAsync);
                    menu.Items.Add(new Separator());

                    if (layer is IEditableLayerInfo e)
                    {
                        var menuImport = new MenuItem() { Header = "导入" };
                        menu.Items.Add(menuImport);
                        if (layer.GeometryType == GeometryType.Polyline
                            || layer.GeometryType == GeometryType.Point
                            || layer.GeometryType == GeometryType.Multipoint)
                        {
                            AddToMenu(menuImport, "GPX轨迹文件",
                                () => IOUtility.GetImportFeaturePath(ImportLayerType.Gpx, MainWindow),
                                p => IOUtility.ImportFeatureAsync(MainWindow, p, e, MapView, ImportLayerType.Gpx),
                                "正在导入GPX轨迹文件");
                        }

                        AddToMenu(menuImport, "CSV文件",
                            () => IOUtility.GetImportFeaturePath(ImportLayerType.Csv, MainWindow),
                            p => IOUtility.ImportFeatureAsync(MainWindow, p, e, MapView, ImportLayerType.Csv),
                            "正在导入CSV文件");
                    }
                    var menuExport = new MenuItem() { Header = "导出" };
                    menu.Items.Add(menuExport);

                    AddToMenu(menuExport, "图层包",
                        () => IOUtility.GetExportLayerPath(layer, ExportLayerType.LayerPackge, MainWindow),
                        p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.LayerPackge),
                        "正在导出图层包");
                    if (Config.Instance.CopyShpFileWhenExport)
                    {
                        AddToMenu(menuExport, "图层包（重建）",
                            () => IOUtility.GetExportLayerPath(layer, ExportLayerType.LayerPackgeRebuild, MainWindow),
                            p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.LayerPackgeRebuild),
                            "正在导出图层包");
                    }
                    if (layer is ShapefileMapLayerInfo)
                    {
                        AddToMenu(menuExport, "GPS工具箱图层包",
                            () => IOUtility.GetExportLayerPath(layer, ExportLayerType.GISToolBoxZip, MainWindow),
                            p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.GISToolBoxZip),
                            "正在导出GPS工具箱图层包");
                    }
                    AddToMenu(menuExport, "KML打包文件",
                        () => IOUtility.GetExportLayerPath(layer, ExportLayerType.KML, MainWindow),
                        p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.KML),
                        "正在导出KML打包文件");
                    AddToMenu(menuExport, "GeoJSON文件",
                        () => IOUtility.GetExportLayerPath(layer, ExportLayerType.GeoJSON, MainWindow),
                        p => IOUtility.ExportLayerAsync(MainWindow, p, layer, MapView.Layers, ExportLayerType.GeoJSON),
                        "正在导出GeoJSON文件");
                    AddToMenu(menuExport, "OpenLayers网络地图",
                        () => IOUtility.GetExportLayerPath(layer, ExportLayerType.OpenLayers, MainWindow),
                        p => ExportOpenLayersLayer(layer,p),
                    "正在导出OpenLayers网络地图");
                }
            }
            else
            {
                if (layers.All(p => p.IsLoaded)
                    && layers.Select(p => p.GeometryType).Distinct().Count() == 1)
                {
                    AddToMenu(menu, "合并", () => LayerUtility.UnionAsync(layers, MapView.Layers));
                }
                AddToMenu(menu, "删除", () => DeleteLayersAsync(layers));
                if (layers.All(p => p.IsLoaded && p is ShapefileMapLayerInfo))
                {
                    AddToMenu(menu, "坐标转换", () => CoordinateTransformateAsync(layers.Cast<IEditableLayerInfo>().ToList()));
                }
            }
            if (menu.Items.Count > 0)
            {
                menu.IsOpen = true;
            }
        }
        private async Task ExportOpenLayersLayer(IMapLayerInfo layer, string path)
        {
            try
            {
                await new OpenLayers(path, Directory.GetFiles("res/openlayers"), Config.Instance.BaseLayers.ToArray(), new[] { layer })
                 .ExportAsync();
                IOUtility.ShowExportedSnackbarAndClickToOpenFolder(path);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
            }
        }
        private Task PopulateAllAsync(IServerBasedLayer s)
        {
            return s.PopulateAllFromServiceAsync();
        }

        private async Task SetWfsLayerAsync(WfsMapLayerInfo layer)
        {
            AddWfsLayerDialog dialog = new AddWfsLayerDialog(MapView.Layers, layer);
            await dialog.ShowAsync();
        }

        private async Task ResetTempLayerAsync(TempMapLayerInfo layer)
        {
            if (await CommonDialog.ShowYesNoDialogAsync("确认重置？", "该操作将会删除所有图形"))
            {
                await layer.ReloadAsync(MapView.Layers);
            }
        }

        private async Task QueryAsync(IMapLayerInfo layer)
        {
            if (layer.NumberOfFeatures == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("该图层没有任何要素");
                return;
            }
            var dialog = new QueryFeaturesDialog(MainWindow, MapView, layer);
            dialog.BringToFront();
        }

        private async Task OpenHistoryDialog(IEditableLayerInfo layer)
        {
            await Task.Yield();
            var dialog = FeatureHistoryDialog.Get(MainWindow, layer, MapView);
            dialog.BringToFront();
        }

        private void AddToMenu<T>(ItemsControl menu, string header, IMapLayerInfo layer, Func<T, Task> func) where T : class
        {
            if (layer is T)
            {
                AddToMenu(menu, header, () => func(layer as T));
            }
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

        private async Task CopyFeaturesAsync(IMapLayerInfo layer)
        {
            SelectLayerDialog dialog = new SelectLayerDialog(MapView.Layers,
                p => p is IEditableLayerInfo && p.GeometryType == MapView.Layers.Selected.GeometryType
                , true);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.CopyAllFeaturesAsync(layer, dialog.SelectedLayer as ShapefileMapLayerInfo);
            }
        }

        private async Task SetShapefileLayerAsync(ShapefileMapLayerInfo layer)
        {
            if (layer.Fields.Length == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("该图层没有自定义字段");
                return;
            }
            MapView.Selection.ClearSelection();
            await CreateLayerDialog.OpenEditDialog(MapView.Layers, layer);
        }

        private async Task SetTempLayerAsync(TempMapLayerInfo layer)
        {
            MapView.Selection.ClearSelection();
            await CreateLayerDialog.OpenEditDialog(MapView.Layers, layer);
        }

        private async Task CreateCopyAsync(IMapLayerInfo layer)
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

        private async Task BufferAsync(IMapLayerInfo layer)
        {
            var dialog = new BufferDialog(MapView.Layers);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.BufferAsync(layer, MapView.Layers, dialog.ToNewLayer ? null : dialog.TargetLayer, dialog.Distance, dialog.Union);
            }
        }

        private async Task ZoomToLayerAsync(IMapLayerInfo layer)
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

        private async Task CoordinateTransformateAsync(IEditableLayerInfo layer)
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

        private async Task CoordinateTransformateAsync(IList<IEditableLayerInfo> layers)
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

        private async Task SetTimeExtentAsync(IHasDefaultFields layer)
        {
            DateRangeDialog dialog = new DateRangeDialog(MapView.Layers.Selected);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.SetTimeExtentAsync(layer as IMapLayerInfo);
            }
        }

        private async Task DeleteLayersAsync(IList<IMapLayerInfo> layers)
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
            MapView.Layers.Save();
        }

        private async Task ReloadLayerAsync(IMapLayerInfo layer)
        {
            await MainWindow.DoAsync(async () =>
            {
                try
                {
                    await layer.ReloadAsync(MapView.Layers);
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "加载失败");
                }
            }, "正在重新加载图层");
        }

        private async Task CopyAttributesAsync(IEditableLayerInfo layer)
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

        private async Task ShowAttributeTableAsync(IMapLayerInfo layer)
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