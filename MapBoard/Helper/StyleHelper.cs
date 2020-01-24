using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using FzLib.UI.Dialog;
using FzLib.IO;
using MapBoard.Common;
using MapBoard.Common.Resource;
using MapBoard.Main.IO;
using MapBoard.Main.Style;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MapBoard.Common.CoordinateTransformation;

namespace MapBoard.Main.Helper
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
                if (template == null)
                {
                    name = "新样式-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                }
                else
                {
                    name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(template.FileName));
                }
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

                var newStyle = CreateStyle(style.Type, style);
                ShapefileFeatureTable targetTable = newStyle.Table;

                //foreach (var feature in features)
                //{
                //    await targetTable.AddFeatureAsync(feature);
                //}
              await  targetTable.AddFeaturesAsync(features);
                newStyle.UpdateFeatureCount();
                style.LayerVisible = false;
            }
            else
            {
                CreateStyle(style.Type, style);
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
                UniqueValueRenderer renderer = new UniqueValueRenderer();
                renderer.FieldNames.Add("Key");
                if (style.Symbols.Count == 0)
                {
                    style.Symbols.Add("", new SymbolInfo());
                }
                foreach (var info in style.Symbols)
                {
                    var key = info.Key;
                    var symbolInfo = info.Value;

                    Symbol symbol = null;

                    switch (style.Layer.FeatureTable.GeometryType)
                    {
                        case GeometryType.Point:
                        case GeometryType.Multipoint:
                            symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, symbolInfo.FillColor, symbolInfo.LineWidth);

                            break;
                        case GeometryType.Polyline:
                            symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, symbolInfo.LineColor, symbolInfo.LineWidth);
                            break;
                        case GeometryType.Polygon:
                            var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, symbolInfo.LineColor, symbolInfo.LineWidth);
                            symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, symbolInfo.FillColor, lineSymbol);
                            break;
                    }

                    if (key == "")
                    {
                        renderer.DefaultSymbol = symbol;
                    }
                    else
                    {
                        renderer.UniqueValues.Add(new UniqueValue(key, key, symbol, key));
                    }

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
            var newStyle = CreateStyle(GeometryType.Polygon, style);

            ShapefileFeatureTable newTable = newStyle.Table;

            foreach (var feature in await style.GetAllFeatures())
            {
                Geometry oldGeometry = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator);
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
            if (style.TimeExtent == null)
            {
                return;
            }
            if (!style.Table.Fields.Any(p => p.FieldType == FieldType.Date && p.Name == Resource.TimeExtentFieldName))
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

        public async static Task<StyleInfo> Union(IEnumerable<StyleInfo> styles)
        {
            if (styles == null || !styles.Any())
            {
                throw new Exception("样式为空");
            }
            var type = styles.Select(p => p.Type).Distinct();
            if (type.Count() != 1)
            {
                throw new Exception("样式的类型并非统一");
            }
            StyleInfo style = CreateStyle(type.First());

            foreach (var oldStyle in styles)
            {
                var oldFeatures = await oldStyle.GetAllFeatures();

                var features = oldFeatures.Select(p => style.Table.CreateFeature(p.Attributes, p.Geometry));
                await style.Table.AddFeaturesAsync(features);
                oldStyle.LabelVisible = false;
            }
            style.UpdateFeatureCount();
            //StyleCollection.Instance.Styles.Add(style);
            return style;

        }


    }
}