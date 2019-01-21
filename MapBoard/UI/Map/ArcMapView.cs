using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic;
using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using FzLib.IO;
using MapBoard.Resource;
using MapBoard.Style;
using MapBoard.UI.Map;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapBoard.UI.Map
{
    public class ArcMapView : MapView, INotifyPropertyChanged
    {
        public ArcMapView()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new Exception("不允许多实例");
            }
            Loaded += ArcMapViewLoaded;
            AllowDrop = true;
            IsAttributionTextVisible = false;
            SketchEditor = new SketchEditor();
            SketchEditor.EditConfiguration.AllowMove = SketchEditor.EditConfiguration.AllowRotate = SketchEditor.EditConfiguration.AllowVertexEditing = true;

            Edit = new EditHelper();
            Selection = new SelectionHelper();
            Drawing = new DrawHelper();
            Layer = new LayerHelper();

            Load().Wait();
        }

        public static ArcMapView Instance { get; private set; }
        public EditHelper Edit { get; private set; }
        public SelectionHelper Selection { get; private set; }
        public DrawHelper Drawing { get; private set; }
        public LayerHelper Layer { get; private set; }
        /// <summary>
        /// 左键抬起事件，用于结束框选
        /// </summary>
        /// <param name="e"></param>
        protected async override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);
            if (SketchEditor.Geometry != null && BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
            {
                await Selection.StopFrameSelect(true);
            }
        }
        /// <summary>
        /// 右键按下事件，用于显示右键菜单，现已废除
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonDown(e);
            //MapPoint point = GeometryEngine.Project(ScreenToLocation(e.GetPosition(this)), SpatialReferences.Wgs84) as MapPoint;
            //foreach (var feature in Selection.SelectedFeatures)
            //{
            //if (Editing.IsEditing)
            //{
            //    ContextMenu menu = new ContextMenu();
            //    MenuItem menuOk = new MenuItem() { Header = "完成" };
            //    menuOk.Click += async (p1, p2) => await Editing.StopEditing();
            //    menu.Items.Add(menuOk);

            //    MenuItem menuReset = new MenuItem() { Header = "还原" };
            //    menuReset.Click += async (p1, p2) => await Editing.AbandonEditing();
            //    menu.Items.Add(menuReset);

            //    if (SketchEditor.SelectedVertex != null)
            //    {
            //        MenuItem menuRemoveVertex = new MenuItem() { Header = "移除节点" };
            //        menuRemoveVertex.Click += (p1, p2) => SketchEditor.RemoveSelectedVertex();
            //        menu.Items.Add(menuRemoveVertex);
            //    }
            //    menu.IsOpen = true;
            //}
            //else
            //{

            //if (IsMouseOverFeature(point, feature))
            //{
            //if (StyleCollection.Instance.Selected != null && Selection.SelectedFeatures.Count > 0)
            //{
            //    ContextMenu menu = new ContextMenu();

            //    MenuItem menuCount = new MenuItem() { Header = $"共{Selection.SelectedFeatures.Count.ToString()}个图形" };
            //    menuCount.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            //    menuCount.FontWeight = FontWeights.Bold;
            //    menu.Items.Add(menuCount);

            //    MenuItem menuDelete = new MenuItem() { Header = "删除" };
            //    menuDelete.Click += async (p1, p2) => await Editing.DeleteSelectedFeatures();
            //    menu.Items.Add(menuDelete);


            //    MenuItem menuCopy = new MenuItem() { Header = "复制" };
            //    menuCopy.Click += MenuCopyClick;
            //    menu.Items.Add(menuCopy);


            //    if (Selection.SelectedFeatures.Count == 1)
            //    {
            //        MenuItem menuEdit = new MenuItem() { Header = "编辑" };
            //        menuEdit.Click += (p1, p2) => Editing.StartEdit(EditHelper.EditMode.Draw);
            //        menu.Items.Add(menuEdit);


            //        if (StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polygon || StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polyline)
            //        {
            //            MenuItem menuCut = new MenuItem() { Header = "切割" };
            //            menuCut.Click += (p1, p2) => Editing.StartEdit(EditHelper.EditMode.Cut);
            //            menu.Items.Add(menuCut);
            //        }
            //    }
            //    if (StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polyline)//线
            //    {
            //        double length = Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
            //        MenuItem menuLength = new MenuItem() { Header = "长度：" + Number.MeterToFitString(length) };
            //        menu.Items.Add(menuLength);
            //    }
            //    else if (StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polyline)//面
            //    {
            //        double length = Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
            //        double area = Selection.SelectedFeatures.Sum(p => GeometryEngine.AreaGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
            //        MenuItem menuLength = new MenuItem() { Header = "周长：" + Number.MeterToFitString(length) };
            //        MenuItem menuArea = new MenuItem() { Header = "面积：" + Number.SquareMeterToFitString(area) };
            //        menu.Items.Add(menuLength);
            //        menu.Items.Add(menuArea);
            //    }




            //    menu.IsOpen = true;
            //    //}
            //}
            //}
        }


        /// <summary>
        /// 键盘按下事件
        /// </summary>
        /// <param name="e"></param>
        protected async override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Delete when SketchEditor.SelectedVertex != null:
                    SketchEditor.RemoveSelectedVertex();
                    break;
                case Key.Delete when BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select:
                    await Edit.DeleteSelectedFeatures();
                    break;

                case Key.Space:
                case Key.Enter:
                    switch (BoardTaskManager.CurrentTask)
                    {
                        case BoardTaskManager.BoardTask.Draw:
                            await Drawing.StopDraw();
                            break;
                        case BoardTaskManager.BoardTask.Edit:
                            await Edit.StopEditing();
                            break;
                        case BoardTaskManager.BoardTask.Ready when Drawing.LastDrawMode.HasValue:
                            await Drawing.StartDraw(Drawing.LastDrawMode.Value);
                            break;
                    }
                    break;

                case Key.Escape when BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw:
                    await Drawing.StopDraw(false);
                    break;
                case Key.Escape when Selection.SelectedFeatures.Count > 0:
                    await Selection.StopFrameSelect(false);
                    break;


                case Key.Z when Keyboard.Modifiers == ModifierKeys.Control && SketchEditor.UndoCommand.CanExecute(null):
                    SketchEditor.UndoCommand.Execute(null);
                    break;
                case Key.Z when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && SketchEditor.RedoCommand.CanExecute(null):
                    SketchEditor.RedoCommand.Execute(null);
                    break;
                case Key.Y when Keyboard.Modifiers == ModifierKeys.Control && SketchEditor.RedoCommand.CanExecute(null):
                    SketchEditor.RedoCommand.Execute(null);
                    break;
            }
            //if (e.Key == Key.Delete)
            //{
            //    await Editing.DeleteSelectedFeatures();
            //}
            //else if (e.Key == Key.Enter)
            //{
            //    if (BoardTaskManager.CurrentTask == BoardTaskManager.OperationTask.Draw)
            //    {
            //        await Drawing.StopDraw();
            //    }
            //    else if (Drawing.LastDrawMode.HasValue)
            //    {
            //        await Drawing.StartDraw(Drawing.LastDrawMode.Value);
            //    }
            //}
            //else if (e.Key == Key.Escape)
            //{
            //    if (BoardTaskManager.CurrentTask == BoardTaskManager.OperationTask.Draw)
            //    {
            //        await Drawing.StopDraw(false);
            //    }
            //    else if (Selection.SelectedFeatures.Count > 0)
            //    {
            //        Selection.ClearSelection();
            //    }
            //}
            //else if (SketchEditor.UndoCommand.CanExecute(null) && e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            //{
            //    SketchEditor.UndoCommand.Execute(null);
            //}
            //else if (SketchEditor.RedoCommand.CanExecute(null) && e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            //{
            //    SketchEditor.RedoCommand.Execute(null);
            //}
            //else if (SketchEditor.RedoCommand.CanExecute(null) && e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            //{
            //    SketchEditor.RedoCommand.Execute(null);
            //}
        }




        private WebTiledLayer baseLayer;
        bool loaded = false;
        /// <summary>
        /// 地图控件加载完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            //await Load();
        }
        /// <summary>
        /// 加载底图和图层事件
        /// </summary>
        /// <returns></returns>
        private async Task Load()
        {
            await LoadBasemap();
            //await Layer.LoadLayers();
        }


        public async Task LoadBasemap()
        {
            if (!Config.Instance.Url.Contains("{x}") || !Config.Instance.Url.Contains("{y}") || !Config.Instance.Url.Contains("{z}"))
            {
                TaskDialog.ShowError("瓦片地址不包含足够的信息！");
                return;
            }

            loaded = true;
            try
            {
                Basemap basemap = new Basemap();
                foreach (var url in Config.Instance.Url.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    baseLayer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
                    basemap.BaseLayers.Add(baseLayer);
                }

                await basemap.LoadAsync();
                if (Map != null)
                {
                    Map.Basemap = basemap;
                }
                else
                {
                    Map = new Esri.ArcGISRuntime.Mapping.Map(basemap);
                }

                await Map.LoadAsync();
            }
            catch (Exception ex)
            {
                TaskDialog.ShowException(ex, "加载地图失败");
                return;
            }

        }



    }
}
