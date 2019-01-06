using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic;
using FzLib.Control.Dialog;
using FzLib.Geography.Format;
using FzLib.IO;
using MapBoard.BoardOperation;
using MapBoard.Code;
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

        public EditHelper Editing { get; private set; }
        public SelectionHelper Selection { get; private set; }

        public DrawHelper Drawing { get; private set; }
        public ArcMapView()
        {
            Loaded += ArcMapViewLoaded;
            AllowDrop = true;
            SketchEditor = new SketchEditor();
            SketchEditor.EditConfiguration.AllowMove = SketchEditor.EditConfiguration.AllowRotate = SketchEditor.EditConfiguration.AllowVertexEditing = true;
            Editing = new EditHelper(this);
            Selection = new SelectionHelper(this);
            Drawing = new DrawHelper(this);
        }

        protected async override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);
            if (SketchEditor.Geometry != null && Selection.IsSelecting)
            {
                await Selection.StopSelect(true);
            }
        }
        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonDown(e);
            //MapPoint point = GeometryEngine.Project(ScreenToLocation(e.GetPosition(this)), SpatialReferences.Wgs84) as MapPoint;
            //foreach (var feature in Selection.SelectedFeatures)
            //{
            if (Editing.IsEditing)
            {

                MenuItem menuDelete = new MenuItem() { Header = "完成" };
                menuDelete.Click += async (p1, p2) => await Editing.StopEditing();

                MenuItem menuEdit = new MenuItem() { Header = "还原" };
                menuEdit.Click += async (p1, p2) => await Editing.ResetEditingFeature();

                ContextMenu menu = new ContextMenu()
                {
                    Items =
                            {
                            menuDelete,
                            menuEdit
                            },
                    IsOpen = true,
                };
            }
            else
            {
                //if (IsMouseOverFeature(point, feature))
                //{
                if (Selection.SelectedFeatures.Count > 0)
                {
                    MenuItem menuDelete = new MenuItem() { Header = "删除" };
                    menuDelete.Click += async (p1, p2) => await Editing.DeleteSelectedFeatures();

                    MenuItem menuEdit = new MenuItem() { Header = "编辑" };
                    menuEdit.Click += (p1, p2) => Editing.EditSelectedFeature();

                    MenuItem menuCopy = new MenuItem() { Header = "复制" };
                    menuCopy.Click += MenuCopyClick;

                    ContextMenu menu = new ContextMenu()
                    {
                        Items =
                            {
                            menuDelete,
                            menuEdit,
                            menuCopy
                            },
                        IsOpen = true,
                    };
                }
            }
            //}
        }

        private async void MenuCopyClick(object sender, RoutedEventArgs e)
        {

            SelectStyleDialog dialog = new SelectStyleDialog(App.Current.MainWindow);
            if (dialog.ShowDialog() == true)
            {
                StyleCollection.Instance.Selected.LayerVisible = false;
                ObservableCollection<Feature> features = Selection.SelectedFeatures;
                ShapefileFeatureTable targetTable = dialog.SelectedStyle.Table;
                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                await Selection.StopSelect(false);
                dialog.SelectedStyle.UpdateFeatureCount();
                StyleCollection.Instance.Selected = dialog.SelectedStyle;
            }
        }

        protected override async void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Delete)
            {
                await Editing.DeleteSelectedFeatures();
            }
            else if (SketchEditor.UndoCommand.CanExecute(null) && e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SketchEditor.UndoCommand.Execute(null);
            }
            else if (SketchEditor.RedoCommand.CanExecute(null) && e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SketchEditor.RedoCommand.Execute(null);
            }
            else if (SketchEditor.RedoCommand.CanExecute(null) && e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                SketchEditor.RedoCommand.Execute(null);
            }
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




        WebTiledLayer baseLayer;
        bool loaded = false;
        private async void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            await LoadBasemap();
            await LoadLayers();


        }

        public async Task LoadLayers()
        {
            Map.OperationalLayers.Clear();
            Selection.SelectedFeatures.Clear();
            Selection.IsSelecting = false;

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
                    if (!StyleCollection.Instance.Styles.Contains(style))
                    {
                        StyleCollection.Instance.Styles.Add(style);
                    }
                    //if (style.FeatureCount == 0)
                    //{
                    //    RemoveStyle(style, true);
                    //}
                    //else
                    //{
                    FeatureLayer layer = new FeatureLayer(featureTable);
                    Map.OperationalLayers.Add(layer);

                    style.Table = featureTable;
                    style.UpdateFeatureCount();
                    SetRenderer(style);
                    //  }
                }
            }
            catch (Exception ex)
            {
                SnakeBar.ShowException(ex, $"无法加载样式{style.Name}");
            }
        }

        public void RemoveStyle(StyleInfo style, bool deleteFiles)
        {
            if (style.Layer != null && Map.OperationalLayers.Contains(style.Layer))
            {
                Map.OperationalLayers.Remove(style.Layer);
            }
            if (StyleCollection.Instance.Styles.Contains(style))
            {
                StyleCollection.Instance.Styles.Remove(style);
            }
            style.Table.Close();

            if (deleteFiles)
            {
                foreach (var file in Directory.EnumerateFiles(Config.DataPath))
                {
                    if (Path.GetFileNameWithoutExtension(file) == style.Name)
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        public StyleInfo GetStyle(StyleInfo template, GeometryType type)
        {
            if (StyleCollection.Instance.Styles.Any(p => p.StyleEquals(template, type)))
            {
                return StyleCollection.Instance.Styles.First(p => p.StyleEquals(template, type));
            }
            else
            {
                string fileName = "新样式-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");

                switch (type)
                {
                    case GeometryType.Point:
                        Shapefile.ExportEmptyPointShapefile(Config.DataPath, fileName);
                        break;
                    case GeometryType.Multipoint:
                        Shapefile.ExportEmptyMultipointShapefile(Config.DataPath, fileName);
                        break;
                    case GeometryType.Polyline:
                        Shapefile.ExportEmptyPolylineShapefile(Config.DataPath, fileName);
                        break;
                    case GeometryType.Polygon:
                        Shapefile.ExportEmptyPolygonShapefile(Config.DataPath, fileName);
                        break;
                }
                StyleInfo style = new StyleInfo();
                style.CopyStyleFrom(template);
                style.Name = fileName;
                StyleCollection.Instance.Styles.Add(style);
                return style;
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

        public void SetRenderer(StyleInfo style)
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

        }

        public async Task<ShapefileFeatureTable> GetFeatureTable(GeometryType type)
        {
            var style = GetStyle(StyleCollection.Instance.Current, type);
            ShapefileFeatureTable table = style.Table;

            if (table == null)
            {
                table = new ShapefileFeatureTable(Config.DataPath + "\\" + style.Name + ".shp");
                await table.LoadAsync();
                if (table.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
                {

                    FeatureLayer layer = new FeatureLayer(table);
                    Map.OperationalLayers.Add(layer);
                    style.Table = table;

                    SetRenderer(style);
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
                TaskDialog.ShowException(App.Current.MainWindow, ex, "加载地图失败");
                return;
            }
            Map = map;

            await SetViewpointCenterAsync(new MapPoint(13532000, 3488400));
            await SetViewpointScaleAsync(1000000);
        }



        public async Task PolylineToPolygon(StyleInfo style)
        {
            StyleInfo newStyle = new StyleInfo();
            newStyle.CopyStyleFrom(style);
            newStyle.Name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(style.FileName));
            Shapefile.ExportEmptyPolygonShapefile(Config.DataPath, newStyle.Name);

            ShapefileFeatureTable newTable = new ShapefileFeatureTable(newStyle.FileName);
            await newTable.LoadAsync();
            QueryParameters query = new QueryParameters
            {
                //Geometry = new Envelope(new MapPoint(-180,-88,SpatialReferences.Wgs84),new MapPoint(180,88, SpatialReferences.Wgs84)),
                SpatialRelationship = SpatialRelationship.Contains
            };

            foreach (var feature in await style.GetAllFeatures())
            {
                Polyline line = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator) as Polyline;
                Feature newFeature = newTable.CreateFeature();
                newFeature.Geometry = GeometryEngine.Buffer(line, Config.Instance.StaticWidth);
                await newTable.AddFeatureAsync(newFeature);

            }

            FeatureLayer layer = new FeatureLayer(newTable);

            newStyle.UpdateFeatureCount();
            Map.OperationalLayers.Add(layer);

            newStyle.Table = newTable;
            SetRenderer(newStyle);
            StyleCollection.Instance.Styles.Add(newStyle);

        }
    }
}
