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
using MapBoard.UI.BoardOperation;
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

namespace MapBoard.UI
{
    public class ArcMapView : MapView, INotifyPropertyChanged
    {
        public static ArcMapView Instance { get; private set; }
        public EditHelper Editing { get; private set; }
        public SelectionHelper Selection { get; private set; }

        public DrawHelper Drawing { get; private set; }
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
            SketchEditor = new SketchEditor();
            SketchEditor.EditConfiguration.AllowMove = SketchEditor.EditConfiguration.AllowRotate = SketchEditor.EditConfiguration.AllowVertexEditing = true;
            Editing = new EditHelper();
            Selection = new SelectionHelper();
            Drawing = new DrawHelper();

            Load();
        }

        protected async override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);
            if (SketchEditor.Geometry != null && BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
            {
                await Selection.StopFrameSelect(true);
            }
        }
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

        //private async void MenuCopyClick(object sender, RoutedEventArgs e)
        //{

        //    SelectStyleDialog dialog = new SelectStyleDialog(App.Current.MainWindow);
        //    if (dialog.ShowDialog() == true)
        //    {
        //        StyleCollection.Instance.Selected.LayerVisible = false;
        //        ObservableCollection<Feature> features = Selection.SelectedFeatures;
        //        ShapefileFeatureTable targetTable = dialog.SelectedStyle.Table;
        //        foreach (var feature in features)
        //        {
        //            await targetTable.AddFeatureAsync(feature);
        //        }
        //        await Selection.StopFrameSelect(false);
        //        dialog.SelectedStyle.UpdateFeatureCount();
        //        StyleCollection.Instance.Selected = dialog.SelectedStyle;
        //    }
        //}

        protected async override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Delete when SketchEditor.SelectedVertex != null:
                    SketchEditor.RemoveSelectedVertex();
                    break;
                case Key.Delete when BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select:
                    await Editing.DeleteSelectedFeatures();
                    break;

                case Key.Space:
                case Key.Enter:
                    switch (BoardTaskManager.CurrentTask)
                    {
                        case BoardTaskManager.BoardTask.Draw:
                            await Drawing.StopDraw();
                            break;
                        case BoardTaskManager.BoardTask.Edit:
                            await Editing.StopEditing();
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

        public bool IsMouseOverFeature(MapPoint point, Feature feature)
        {
            //if (feature.Geometry.GeometryType == GeometryType.Polygon)
            //{
            //    if (GeometryEngine.Contains(feature.Geometry, point))
            //    {
            //        return true;
            //    }
            //    return false;
            //}

            double tolerance = MapScale / 1e8;
            Envelope envelope = new Envelope(point.X - tolerance, point.Y - tolerance, point.X + tolerance, point.Y + tolerance, SpatialReferences.Wgs84);
            return GeometryEngine.Intersects(feature.Geometry, envelope);

        }


        private WebTiledLayer baseLayer;
        bool loaded = false;
        private void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            //await Load();
        }

        private async Task Load()
        {
            await LoadBasemap();
            await LoadLayers();

        }

        public async Task LoadLayers()
        {
            Selection.SelectedFeatures.Clear();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;

            if (!Directory.Exists(Config.DataPath))
            {
                Directory.CreateDirectory(Config.DataPath);
                return;
            }

            foreach (var style in StyleCollection.Instance.Styles.ToArray())
            {
                if (File.Exists(Path.Combine(Config.DataPath, style.Name + ".shp")))
                {
                    await LoadLayer(style);
                }
                else
                {
                    StyleCollection.Instance.Styles.Remove(style);
                }
            }

            HashSet<string> files = Directory.EnumerateFiles(Config.DataPath)
                .Where(p => Path.GetExtension(p) == ".shp")
                .Select(p =>
                {
                    int index = p.LastIndexOf('.');
                    if (index == -1)
                    {
                        return p;
                    }
                    return p.Remove(index, p.Length - index).RemoveStart(Config.DataPath + "\\");
                }).ToHashSet();

            foreach (var name in files)
            {
                if (!StyleCollection.Instance.Styles.Any(p => p.Name == name))
                {
                    StyleInfo style = new StyleInfo();
                    style.Name = name;
                    await LoadLayer(style);
                }
            }
        }

        public async Task LoadLayer(StyleInfo style)
        {
            try
            {
                ShapefileFeatureTable featureTable = new ShapefileFeatureTable(Config.DataPath + "\\" + style.Name + ".shp");
                await featureTable.LoadAsync();
                if (featureTable.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
                {
                    //if (!StyleCollection.Instance.Styles.Contains(style))
                    //{
                    //    StyleCollection.Instance.Styles.Add(style);
                    //}
                    //if (style.FeatureCount == 0)
                    //{
                    //    RemoveStyle(style, true);
                    //}
                    //else
                    //{
                    //FeatureLayer layer = new FeatureLayer(featureTable);
                    ////Map.OperationalLayers.Add(layer);

                    //style.Table = featureTable;
                    //style.UpdateFeatureCount();
                    //SetRenderer(style);
                    //  }
                }
            }
            catch (Exception ex)
            {
                if (SnakeBar.DefaultWindow == null)
                {
                    TaskDialog.ShowException(ex, $"无法加载样式{style.Name}");
                }
                else
                {
                    SnakeBar.ShowException(ex, $"无法加载样式{style.Name}");

                }
            }
        }


        //public StyleInfo GetStyleFromConfig(string name)
        //{
        //    StyleInfo style = Config.Instance.ShapefileStyles.FirstOrDefault(p => p.Name == name)?.Clone();
        //    if (style == null)
        //    {
        //        style = Config.Instance.DefaultStyle.Clone();
        //        style.Name = name;
        //        Config.Instance.AddToShapefileStyles(style.Clone());
        //    }

        //    return style;
        //    //try
        //    //{
        //    //    StyleInfo style = new StyleInfo();
        //    //    if (name.StartsWith("Polyline"))
        //    //    {
        //    //        string[] parts = name.Split('_', '.');
        //    //        style.LineWidth = double.Parse(parts[1]) / 100;
        //    //        style.LineColor = System.Drawing.Color.FromArgb(int.Parse(parts[2]));
        //    //    }
        //    //    else if (name.StartsWith("Polygon"))
        //    //    {
        //    //        string[] parts = name.Split('_', '.');
        //    //        style.LineWidth = double.Parse(parts[1]) / 100;
        //    //        style.LineColor = System.Drawing.Color.FromArgb(int.Parse(parts[2]));
        //    //        style.FillColor = System.Drawing.Color.FromArgb(int.Parse(parts[3]));
        //    //    }
        //    //    else if (name.StartsWith("Point"))
        //    //    {
        //    //        string[] parts = name.Split('_', '.');
        //    //        style.LineWidth = double.Parse(parts[1]) / 100;
        //    //        style.FillColor = System.Drawing.Color.FromArgb(int.Parse(parts[2]));
        //    //    }
        //    //    return style;
        //    //}
        //    //catch
        //    //{
        //    //    return null;
        //    //}
        //}

        public void ApplyStyles(StyleInfo style)
        {
            try
            {
                SimpleLineSymbol lineSymbol;
                SimpleRenderer renderer = null;
                switch (style.Layer.FeatureTable.GeometryType)
                {
                    case GeometryType.Point:
                    case GeometryType.Multipoint:
                        SimpleMarkerSymbol markerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, style.FillColor, style.LineWidth);
                        renderer = new SimpleRenderer(markerSymbol);
                        break;
                    case GeometryType.Polyline:
                        lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, style.LineColor, style.LineWidth);
                        renderer = new SimpleRenderer(lineSymbol);
                        break;
                    case GeometryType.Polygon:
                        lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, style.LineColor, style.LineWidth);
                        SimpleFillSymbol fillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, style.FillColor, lineSymbol);
                        renderer = new SimpleRenderer(fillSymbol);
                        break;
                }
                style.Layer.Renderer = renderer;

                string labelJson = style.LabelJson;
                LabelDefinition labelDefinition = LabelDefinition.FromJson(labelJson);
                style.Layer.LabelDefinitions.Clear();
                style.Layer.LabelDefinitions.Add(labelDefinition);
                style.Layer.LabelsEnabled = true;
            }
            catch (Exception ex)
            {
                string error = (string.IsNullOrWhiteSpace(style.Name) ? "图层" + style.Name : "图层") + "样式加载失败";
                TaskDialog.ShowException(ex, error);
            }
        }

        public async Task<ShapefileFeatureTable> GetFeatureTable(GeometryType type)
        {
            var style = StyleCollection.Instance.Selected;// StyleHelper.GetStyle(StyleCollection.Instance.Selected, type);
            ShapefileFeatureTable table = style.Table;

            if (table == null)
            {
                table = new ShapefileFeatureTable(Config.DataPath + "\\" + style.Name + ".shp");
                await table.LoadAsync();
                if (table.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
                {

                    FeatureLayer layer = new FeatureLayer(table);
                    //Map.OperationalLayers.Add(layer);
                    style.Table = table;

                    ApplyStyles(style);
                }
            }
            return table;
        }

        public async Task LoadBasemap()
        {

            loaded = true;
            baseLayer = new WebTiledLayer(Config.Instance.Url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));

            Basemap basemap = new Basemap(baseLayer);
            await basemap.LoadAsync();
            Map map = new Map(basemap);
            try
            {
                await map.LoadAsync();
            }
            catch (Exception ex)
            {
                TaskDialog.ShowException(ex, "加载地图失败");
                return;
            }
            Map = map;

            //await SetViewpointCenterAsync(new MapPoint(13532000, 3488400));
            //await SetViewpointScaleAsync(1000000);
        }

        public async Task PolylineToPolygon(StyleInfo style)
        {
            var newStyle = StyleHelper.CreateStyle(GeometryType.Polygon, style, Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(style.FileName)));

            ShapefileFeatureTable newTable = newStyle.Table;
            await newTable.LoadAsync();

            foreach (var feature in await style.GetAllFeatures())
            {
                Polyline line = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator) as Polyline;
                Feature newFeature = newTable.CreateFeature();
                newFeature.Geometry = GeometryEngine.Buffer(line, Config.Instance.StaticWidth);
                await newTable.AddFeatureAsync(newFeature);

            }
            newStyle.Table = newTable;
            //SetRenderer(newStyle);
            StyleCollection.Instance.Styles.Add(newStyle);

        }

        public async void AddLayer(StyleInfo style)
        {
            try
            {

                if (style.Table == null)
                {
                    style.Table = new ShapefileFeatureTable(style.FileName);
                    await style.Table.LoadAsync();
                }
                FeatureLayer layer = new FeatureLayer(style.Table);
                Map.OperationalLayers.Add(layer);
                ApplyStyles(style);
                style.LoadLayerVisibility();
            }
            catch (Exception ex)
            {
                string error = (string.IsNullOrWhiteSpace(style.Name) ? "图层" + style.Name : "图层") + "加载失败";
                TaskDialog.ShowException(ex, error);
            }
        }

        public void RemoveLayer(StyleInfo style)
        {
            try
            {
                Map.OperationalLayers.Remove(style.Layer);
                style.Table.Close();
            }
            catch
            {

            }
        }

        public void ClearLayers()
        {
            foreach (var layer in Map.OperationalLayers.ToArray())
            {
                Map.OperationalLayers.Remove(layer);
                ((layer as FeatureLayer).FeatureTable as ShapefileFeatureTable).Close();
            }
        }
    }
}
