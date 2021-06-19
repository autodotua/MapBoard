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

namespace MapBoard.UI
{
    public class LayerListPanelHelper
    {
        private readonly ListView list;

        public MainWindow MainWindow { get; }
        public ArcMapView MapView { get; }

        public LayerListPanelHelper(ListView list, MainWindow win, ArcMapView mapView)
        {
            this.list = list;
            MainWindow = win;
            MapView = mapView;

            //为图层列表提供拖动改变次序支持
            var lvwHelper = new LayerListViewHelper(list);
            lvwHelper.EnableDragAndDropItem();
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
                AddToMenu(menu, "删除", () => DeleteLayersAsync(layers));
                AddToMenu(menu, "新建副本", () => CreateCopyAsync(layer));
                AddToMenu(menu, "编辑字段显示名", () => EditFieldDisplayAsync(layer));
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
                        p => IOUtility.ImportFeatureAsync(p, layer, MapView, ImportLayerType.Gpx),
                        "正在导入GPX轨迹文件");
                }

                AddToMenu(menuImport, "CSV文件",
                    () => IOUtility.GetImportFeaturePath(ImportLayerType.Csv),
                    p => IOUtility.ImportFeatureAsync(p, layer, MapView, ImportLayerType.Csv),
                    "正在导入CSV文件");

                var menuExport = new MenuItem() { Header = "导出" };
                menu.Items.Add(menuExport);

                AddToMenu(menuExport, "图层包",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.LayerPackge),
                    p => IOUtility.ExportLayerAsync(p, layer, MapView.Layers, ExportLayerType.LayerPackge),
                    "正在导出图层包");
                AddToMenu(menuExport, "GPS工具箱图层包",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.GISToolBoxZip),
                    p => IOUtility.ExportLayerAsync(p, layer, MapView.Layers, ExportLayerType.GISToolBoxZip),
                    "正在导出GPS工具箱图层包");
                AddToMenu(menuExport, "KML打包文件",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.KML),
                    p => IOUtility.ExportLayerAsync(p, layer, MapView.Layers, ExportLayerType.KML),
                    "正在导出KML打包文件");
                AddToMenu(menuExport, "GeoJSON文件",
                    () => IOUtility.GetExportLayerPath(layer, ExportLayerType.GeoJSON),
                    p => IOUtility.ExportLayerAsync(p, layer, MapView.Layers, ExportLayerType.GeoJSON),
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
            var dialog = new QueryFeaturesDialog(MainWindow, MapView, layer);
            dialog.BringToFront();
        }

        private async Task OpenHistoryDialog(MapLayerInfo layer)
        {
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
                await FeatureUtility.CopyAttributesAsync(layer, dialog.FieldSource, dialog.FieldTarget, dialog.DateFormat);
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

        public class LayerListViewHelper : ItemControlHelper<ListView, System.Windows.Controls.ListViewItem>
        {
            public LayerListViewHelper(ListView view) : base(view)
            {
            }

            protected override IList GetSelectedItems()
            {
                return View.SelectedItems;
            }
        }

        public class LayerDataGridHelper : ItemControlHelper<DataGrid, DataGridRow>
        {
            public LayerDataGridHelper(DataGrid view) : base(view)
            {
                view.PreparingCellForEdit += CellEditBeginning;
                view.CellEditEnding += CellEditEnding;
            }

            public bool IsCellEditing { get; private set; }

            private void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
            {
                IsCellEditing = false;
                EditingCellLocation = null;
                EditingCell = null;
            }

            private void CellEditBeginning(object sender, DataGridPreparingCellForEditEventArgs e)
            {
                IsCellEditing = true;
                EditingCellLocation = (e.Row, e.Column);
                EditingCell = e.EditingElement;
            }

            public (DataGridRow Row, DataGridColumn Column)? EditingCellLocation { get; private set; }
            public FrameworkElement EditingCell { get; private set; }

            protected override IList GetSelectedItems()
            {
                return View.SelectedItems;
            }

            public bool IsEditing => GetEditingRow() != null;
            public override bool CanDragDrop => !IsCellEditing;

            public DataGridRow GetEditingRow()
            {
                var index = View.SelectedIndex;
                if (index >= 0)
                {
                    DataGridRow selected = GetItem(index);
                    if (selected.IsEditing) return selected;
                }

                for (int i = 0; i < View.Items.Count; i++)
                {
                    if (i == index) continue;
                    var item = GetItem(i);
                    if (item.IsEditing) return item;
                }

                return null;
            }
        }

        public abstract class ItemControlHelper<TView, TViewItem> where TView : Selector where TViewItem : Control
        {
            public TView View { get; private set; }

            public ItemControlHelper(TView view)
            {
                if (!(view is MultiSelector
                    || view is ListBox))
                {
                    throw new Exception("不支持的View");
                }
                View = view;
            }

            public void EnableDragAndDropItem()
            {
                View.AllowDrop = true;
                View.MouseMove += SingleMouseMove;
                View.Drop += SingleDrop;
            }

            public void EnableDragAndDropItems()
            {
                View.AllowDrop = true;
                View.MouseMove += MultiMouseMove;
                View.Drop += MultiDrop;
            }

            public void DisableDragAndDropItems()
            {
                View.AllowDrop = true;
                View.MouseMove -= MultiMouseMove;
                View.Drop -= MultiDrop;
            }

            private void SingleMouseMove(object sender, MouseEventArgs e)
            {
                if (!CanDragDrop)
                {
                    return;
                }
                TView listview = sender as TView;
                MapLayerInfo select = (MapLayerInfo)listview.SelectedItem;
                if (listview.SelectedIndex < 0)
                {
                    return;
                }
                if (e.LeftButton == MouseButtonState.Pressed && IsMouseOverTarget(GetItem(listview.SelectedIndex), new GetPositionDelegate(e.GetPosition)))
                {
                    DataObject data = new DataObject(typeof(MapLayerInfo), select);

                    DragDrop.DoDragDrop(listview, data, DragDropEffects.Move);
                }
            }

            public virtual bool CanDragDrop => true;

            private void SingleDrop(object sender, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(typeof(MapLayerInfo)))
                {
                    MapLayerInfo item = (MapLayerInfo)e.Data.GetData(typeof(MapLayerInfo));
                    //index为放置时鼠标下元素项的索引
                    int index = GetCurrentIndex(new GetPositionDelegate(e.GetPosition));
                    if (index > -1)
                    {
                        //拖动元素集合的第一个元素索引
                        int oldIndex = (View.ItemsSource as MapLayerCollection).IndexOf(item);
                        if (oldIndex == index)
                        {
                            return;
                        }

                        (View.ItemsSource as MapLayerCollection).Move(oldIndex, index);
                        SingleItemDragDroped?.Invoke(this, new SingleItemDragDropedEventArgs(oldIndex, index));
                    }
                }
            }

            private void MultiMouseMove(object sender, MouseEventArgs e)
            {
                //TView listview = sender as TView;
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    IList list = GetSelectedItems();
                    DataObject data = new DataObject(typeof(IList), list);
                    if (list.Count > 0)
                    {
                        DragDrop.DoDragDrop(View, data, DragDropEffects.Move);
                    }
                }
            }

            private void MultiDrop(object sender, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(typeof(IList)))
                {
                    IList peopleList = e.Data.GetData(typeof(IList)) as IList;
                    //index为放置时鼠标下元素项的索引
                    int index = GetCurrentIndex(new GetPositionDelegate(e.GetPosition));
                    if (index > -1)
                    {
                        MapLayerInfo Logmess = (MapLayerInfo)peopleList[0];
                        //拖动元素集合的第一个元素索引
                        int OldFirstIndex = (View.ItemsSource as MapLayerCollection).IndexOf(Logmess);
                        for (int i = 0; i < peopleList.Count; i++)
                        {
                            (View.ItemsSource as MapLayerCollection).Move(OldFirstIndex, index);
                        }
                        GetSelectedItems().Clear();
                    }
                }
            }

            private int GetCurrentIndex(GetPositionDelegate getPosition)
            {
                int index = -1;
                for (int i = 0; i < View.Items.Count; ++i)
                {
                    TViewItem item = GetItem(i);
                    if (item != null && IsMouseOverTarget(item, getPosition))
                    {
                        index = i;
                        break;
                    }
                }
                return index;
            }

            private bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
            {
                if (target == null)
                {
                    return false;
                }
                Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
                Point mousePos = getPosition((IInputElement)target);
                return bounds.Contains(mousePos);
            }

            private delegate Point GetPositionDelegate(IInputElement element);

            public TViewItem GetItem(int index)
            {
                if (View.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    return null;
                }
                if (index < 0)
                {
                    return null;
                }
                return View.ItemContainerGenerator.ContainerFromIndex(index) as TViewItem;
            }

            protected abstract IList GetSelectedItems();

            public delegate void SingleItemDragDropedEventHandler(object sender, SingleItemDragDropedEventArgs e);

            public event SingleItemDragDropedEventHandler SingleItemDragDroped;
        }

        public class SingleItemDragDropedEventArgs : EventArgs
        {
            public SingleItemDragDropedEventArgs(int oldIndex, int newIndex)
            {
                OldIndex = oldIndex;
                NewIndex = newIndex;
            }

            public int OldIndex { get; private set; }
            public int NewIndex { get; private set; }
        }
    }
}