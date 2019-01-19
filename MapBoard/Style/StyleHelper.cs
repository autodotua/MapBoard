using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using FzLib.Control.Dialog;
using FzLib.IO;
using MapBoard.IO;
using MapBoard.Resource;
using MapBoard.UI;
using MapBoard.UI.Dialog;
using MapBoard.UI.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MapBoard.IO.CoordinateTransformation;

namespace MapBoard.Style
{
    public static class StyleHelper
    {

        public static void RemoveStyle(StyleInfo style, bool deleteFiles)
        {
            if (StyleCollection.Instance.Styles.Contains(style))
            {
                StyleCollection.Instance.Styles.Remove(style);
            }


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

        public static StyleInfo CreateStyle(GeometryType type, StyleInfo template = null, string name = null)
        {
            if (name == null)
            {
                name = "新样式-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            }

            Shapefile.ExportEmptyShapefile(type, name);
            StyleInfo style = new StyleInfo();
            if (template != null)
            {
                style.CopyStyleFrom(template);
            }
            style.Name = name;
            StyleCollection.Instance.Styles.Add(style);
            StyleCollection.Instance.Selected = style;
            return style;
        }

        public async static Task CreatCopy(StyleInfo style, bool includeFeatures)
        {
            if (includeFeatures)
            {

                FeatureQueryResult features = await StyleCollection.Instance.Selected.GetAllFeatures();

                style = StyleHelper.CreateStyle(style.Type, style);
                ShapefileFeatureTable targetTable = style.Table;

                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                style.UpdateFeatureCount();
            }
            else
            {

                StyleHelper.CreateStyle(style.Type, style);
            }
        }

        public async static Task CopyAllFeatures(StyleInfo source, StyleInfo target)
        {

            FeatureQueryResult features = await source.GetAllFeatures();
            ShapefileFeatureTable targetTable = target.Table;

            foreach (var feature in features)
            {
                await targetTable.AddFeatureAsync(feature);
            }
            target.UpdateFeatureCount();
        }

        public async static void CopyFeatures()
        {
            SelectStyleDialog dialog = new SelectStyleDialog();
            if (dialog.ShowDialog() == true)
            {
                await CopyAllFeatures(StyleCollection.Instance.Selected, dialog.SelectedStyle);
            }
        }
        public async static void PolylineToPolygon()
        {
            if (StyleCollection.Instance.Selected == null)
            {
                return;
            }
            if (StyleCollection.Instance.Selected.Table.GeometryType != GeometryType.Polyline)
            {
                SnakeBar.ShowError("只有线可以执行此命令");
                return;
            }
            await PolylineToPolygon(StyleCollection.Instance.Selected);
        }

        public async static void CreateCopy()
        {
            bool includeFeatures = false;
            TaskDialog.ShowWithCommandLinks("是否要复制所有图形到新的样式中", "请选择副本类型", new (string, string, Action)[]
            {
                ("仅样式",null,null ),
                ("样式和所有图形",null,()=>includeFeatures=true)
            });
            await CreatCopy(StyleCollection.Instance.Selected, includeFeatures);
        }

        public static void ApplyStyles(StyleInfo style)
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
                //style.Layer.LabelsEnabled = true;
            }
            catch (Exception ex)
            {
                string error = (string.IsNullOrWhiteSpace(style.Name) ? "图层" + style.Name : "图层") + "样式加载失败";
                TaskDialog.ShowException(ex, error);
            }
        }

        public async static Task PolylineToPolygon(StyleInfo style)
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

        public async static Task CoordinateTransformate(StyleInfo style, string from, string to)
        {
            if(!CoordinateSystems.Contains(from) || !CoordinateSystems.Contains(to))
            {
                throw new ArgumentException("不能识别坐标系");
            }

            CoordinateTransformation coordinate = new CoordinateTransformation(from, to);

            FeatureQueryResult features = await style.GetAllFeatures();

            foreach (var feature in features)
            {
                coordinate.Transformate(feature);
                await style.Table.UpdateFeatureAsync(feature);
            }

        }


    }
}