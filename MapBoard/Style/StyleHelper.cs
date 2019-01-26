using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using FzLib.Control.Dialog;
using FzLib.IO;
using MapBoard.Common;
using MapBoard.Common.Resource;
using MapBoard.Main.IO;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MapBoard.Main.IO.CoordinateTransformation;

namespace MapBoard.Main.Style
{
    public static class StyleHelper
    {

        public static void RemoveStyle(this StyleInfo style, bool deleteFiles)
        {
            if (StyleCollection.Instance.Styles.Contains(style))
            {
                StyleCollection.Instance.Styles.Remove(style);
            }


            if (deleteFiles)
            {
                foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath, style.Name))
                {
                    if (Path.GetFileNameWithoutExtension(file) == style.Name)
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        public static StyleInfo AddStyle(string name)
        {
            StyleInfo style = new StyleInfo();
            style.Name = name ?? throw new ArgumentException();
            StyleCollection.Instance.Styles.Add(style);
            StyleCollection.Instance.Selected = style;
            return style;
        }

        public static StyleInfo CreateStyle(GeometryType type, StyleInfo template = null, string name = null)
        {
            if (name == null)
            {
                name = "新样式-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            }

            ShapefileExport.ExportEmptyShapefile(type, name);
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

        public async static Task CreatCopy(this StyleInfo style, bool includeFeatures)
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
        public async static void Buffer()
        {
            await Buffer(StyleCollection.Instance.Selected);
        }

        public async static void CreateCopy()
        {
            int mode = 0;
            TaskDialog.ShowWithCommandLinks("是否要复制所有图形到新的样式中", "请选择副本类型", new (string, string, Action)[]
            {
                ("仅样式",null,()=>mode=1),
                ("样式和所有图形",null,()=>mode=2)
            }, null, Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.Information, true);
            if (mode > 0)
            {
                await CreatCopy(StyleCollection.Instance.Selected, mode == 2);
            }
        }

        public static void ApplyStyles(this StyleInfo style)
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

        public async static Task Buffer(this StyleInfo style)
        {
            var newStyle = CreateStyle(GeometryType.Polygon, style, Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(style.FileName)));

            ShapefileFeatureTable newTable = newStyle.Table;

            foreach (var feature in await style.GetAllFeatures())
            {
                Geometry oldGeometry = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator) ;
                var geometry = GeometryEngine.Buffer(oldGeometry, Config.Instance.StaticWidth);
                Feature newFeature = newTable.CreateFeature(feature.Attributes, geometry);
                await newTable.AddFeatureAsync(newFeature);

            }

        }

        public async static Task CoordinateTransformate(this StyleInfo style, string from, string to)
        {
            if (!CoordinateSystems.Contains(from) || !CoordinateSystems.Contains(to))
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

        public async static Task SetTimeExtent(this StyleInfo style)
        {
            if(style.TimeExtent==null)
            {
                return;
            }
            if(!style.Table.Fields.Any(p => p.FieldType == FieldType.Date && p.Name == Resource.TimeExtentFieldName))
            {
                throw new Exception("shapefile没有指定的日期属性");
            }
            FeatureLayer layer = style.Layer;

            if (style.TimeExtent.IsEnable)
            {
                List<Feature> visiableFeatures = new List<Feature>();
                List<Feature> invisiableFeatures = new List<Feature>();

                FeatureQueryResult features = await style.GetAllFeatures();

                foreach (var feature in features)
                {
                    if (feature.Attributes[Resource.TimeExtentFieldName] is DateTimeOffset date)
                    {
                        if (date > style.TimeExtent.From && date < style.TimeExtent.To)
                        {
                            visiableFeatures.Add(feature);
                        }
                        else
                        {
                            invisiableFeatures.Add(feature);
                        }
                    }
                    else
                    {
                        invisiableFeatures.Add(feature);
                    }
                }
                
                layer.SetFeaturesVisible(visiableFeatures, true);
                layer.SetFeaturesVisible(invisiableFeatures, false);
            }
            else
            {
                layer.SetFeaturesVisible(await style.GetAllFeatures(), true);

            }
        }




    }
}