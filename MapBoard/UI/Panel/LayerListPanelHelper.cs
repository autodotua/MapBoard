using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common;

using MapBoard.Common;

using MapBoard.Main.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.Util;
using ModernWpf.Controls;
using ModernWpf.FzExtension.CommonDialog;

using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;

using System.Linq;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static FzLib.Basic.Loop;
using MapBoard.Main.UI.Map;

namespace MapBoard.Main.UI.Panel
{
    public class LayerListPanelHelper
    {
        private readonly DataGrid dataGrid;

        public MainWindow Window { get; }

        public LayerListPanelHelper(DataGrid dataGrid, MainWindow window)
        {
            this.dataGrid = dataGrid;
            Window = window;
        }

        public void ShowContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            List<(string header, Func<LayerInfo, Task> action, bool visiable)?> menus = null;
            LayerInfo layer = LayerCollection.Instance.Selected;
            LayerInfo[] layers = dataGrid.SelectedItems.Cast<LayerInfo>().ToArray();
            if (dataGrid.SelectedItems.Count == 1)
            {
                menus = new List<(string header, Func<LayerInfo, Task> action, bool visiable)?>()
               {
                    ("缩放到图层", ZoomToLayerAsync,layer.FeatureCount > 0),
                    ("属性表", ShowAttributeTableAsync,true),
                    ("复制", CopyFeaturesAsync,true),
                    ("删除",async l=>await DeleteSelectedLayersAsync(),true),
                    null,
                    ("建立缓冲区",BufferAsync,layer.Type==GeometryType.Polyline || layer.Type==GeometryType.Point|| layer.Type==GeometryType.Multipoint),
                    ("新建副本", CreateCopyAsync,true),
                    ("坐标转换",CoordinateTransformateAsync,true),
                    ("设置时间范围",SetTimeExtentAsync,layer.Table.Fields.Any(p=>p.FieldType==FieldType.Date && p.Name==Resource.DateFieldName)),
                    ("字段赋值",CopyAttributesAsync,true),
                    null,
                    ("导入",async l=>await IOUtility.ImportFeatureAsync(),true),
                    ("导出",  ExportSingleAsync,true),
               };
            }
            else
            {
                menus = new List<(string header, Func<LayerInfo, Task> action, bool visiable)?>()
               {
                    ("合并",async l=>await LayerUtility. UnionAsync(layers)
                    ,layers.Select(p=>p.Type).Distinct().Count()==1),
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
                                await Window.DoAsync(() => m.Value.action(layer));
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
            SelectLayerDialog dialog = new SelectLayerDialog();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.CopyAllFeaturesAsync(layer, dialog.SelectedLayer);
            }
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
                await LayerUtility.CreatCopyAsync(layer, mode == 2);
            }
        }

        private async static Task BufferAsync(LayerInfo layer)
        {
            await LayerUtility.BufferAsync(layer);
        }

        private Task ExportSingleAsync(LayerInfo layer)
        {
            return IOUtility.ExportLayerAsync(layer);
        }

        private async Task ZoomToLayerAsync(LayerInfo layer)
        {
            try
            {
                await ArcMapView.Instance.ZoomToGeometryAsync(await layer.Table.QueryExtentAsync(new QueryParameters()));
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
                await Window.DoAsync(async () =>
                {
                    string from = dialog.SelectedCoordinateSystem1;
                    string to = dialog.SelectedCoordinateSystem2;
                    await LayerUtility.CoordinateTransformateAsync(layer, from, to);
                });
            }
        }

        private async Task SetTimeExtentAsync(LayerInfo layer)
        {
            DateRangeDialog dialog = new DateRangeDialog(LayerCollection.Instance.Selected);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await LayerUtility.SetTimeExtentAsync(layer);
            }
        }

        private async Task DeleteSelectedLayersAsync()
        {
            if (dataGrid.SelectedItems.Count == 0)
            {
                SnakeBar.ShowError("没有选择任何样式");
                return;
            }
            foreach (LayerInfo layer in dataGrid.SelectedItems.Cast<LayerInfo>().ToArray())
            {
                await LayerUtility.RemoveLayerAsync(layer, true);
            }
        }

        private async Task CopyAttributesAsync(LayerInfo layer)
        {
            var dialog = new CopyAttributesDialog(layer);
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await FieldUtility.CopyAttributesAsync(layer, dialog.FieldSource, dialog.FieldTarget, dialog.DateFormat);
            }
        }

        private async Task ShowAttributeTableAsync(LayerInfo layer)
        {
            var dialog = new AttributeTableDialog(layer) { Owner = Window };
            try
            {
                await Window.DoAsync(dialog.LoadAsync);
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
            if (dataGrid.SelectedItems.Count > 1)
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
                dataGrid.SelectedItem = layer;
            }
        }
    }
}