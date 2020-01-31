using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using FzLib.UI.Dialog;
using FzLib.IO;
using MapBoard.Common;
using MapBoard.Common.Resource;
using MapBoard.Main.IO;
using MapBoard.Main.Layer;
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
using LayerCollection = MapBoard.Main.Layer.LayerCollection;

namespace MapBoard.Main.Helper
{
    public static class LayerHelper
    {

        public static void RemoveLayer(this LayerInfo layer, bool deleteFiles)
        {
            if (LayerCollection.Instance.Layers.Contains(layer))
            {
                LayerCollection.Instance.Layers.Remove(layer);
            }


            if (deleteFiles)
            {
                foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath, layer.Name))
                {
                    if (Path.GetFileNameWithoutExtension(file) == layer.Name)
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        public static LayerInfo AddLayer(string name)
        {
            LayerInfo layer = new LayerInfo();
            layer.Name = name ?? throw new ArgumentException();
            LayerCollection.Instance.Layers.Add(layer);
            LayerCollection.Instance.Selected = layer;
            return layer;
        }

        public static LayerInfo CreateLayer(GeometryType type, LayerInfo template = null, string name = null)
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
            LayerInfo layer = new LayerInfo();
            if (template != null)
            {
                layer.CopyLayerFrom(template);
            }
            layer.Name = name;
            LayerCollection.Instance.Layers.Add(layer);
            LayerCollection.Instance.Selected = layer;
            return layer;
        }

        public async static Task CreatCopy(this LayerInfo layer, bool includeFeatures)
        {
            if (includeFeatures)
            {

                FeatureQueryResult features = await LayerCollection.Instance.Selected.GetAllFeatures();

                var newLayer = CreateLayer(layer.Type, layer);
                ShapefileFeatureTable targetTable = newLayer.Table;

                //foreach (var feature in features)
                //{
                //    await targetTable.AddFeatureAsync(feature);
                //}
              await  targetTable.AddFeaturesAsync(features);
                newLayer.UpdateFeatureCount();
                layer.LayerVisible = false;
            }
            else
            {
                CreateLayer(layer.Type, layer);
            }
        }

        public async static Task CopyAllFeatures(LayerInfo source, LayerInfo target)
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
            SelectLayerDialog dialog = new SelectLayerDialog();
            if (dialog.ShowDialog() == true)
            {
                await CopyAllFeatures(LayerCollection.Instance.Selected, dialog.SelectedStyle);
            }
        }
        public async static void Buffer()
        {
            await Buffer(LayerCollection.Instance.Selected);
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
                await CreatCopy(LayerCollection.Instance.Selected, mode == 2);
            }
        }

        public static void ApplyLayers(this LayerInfo layer)
        {
            try
            {
                UniqueValueRenderer renderer = new UniqueValueRenderer();
                renderer.FieldNames.Add("Key");
                if (layer.Symbols.Count == 0)
                {
                    layer.Symbols.Add("", new SymbolInfo());
                }
                foreach (var info in layer.Symbols)
                {
                    var key = info.Key;
                    var symbolInfo = info.Value;

                    Symbol symbol = null;

                    switch (layer.Layer.FeatureTable.GeometryType)
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

                layer.Layer.Renderer = renderer;
                string labelJson = layer.LabelJson;
                LabelDefinition labelDefinition = LabelDefinition.FromJson(labelJson);
                layer.Layer.LabelDefinitions.Clear();
                layer.Layer.LabelDefinitions.Add(labelDefinition);
                //style.Layer.LabelsEnabled = true;
            }
            catch (Exception ex)
            {
                string error = (string.IsNullOrWhiteSpace(layer.Name) ? "图层" + layer.Name : "图层") + "样式加载失败";
                TaskDialog.ShowException(ex, error);
            }
        }

        public async static Task Buffer(this LayerInfo layer)
        {
            var newLayer = CreateLayer(GeometryType.Polygon, layer);

            ShapefileFeatureTable newTable = newLayer.Table;

            foreach (var feature in await layer.GetAllFeatures())
            {
                Geometry oldGeometry = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator);
                var geometry = GeometryEngine.Buffer(oldGeometry, Config.Instance.StaticWidth);
                Feature newFeature = newTable.CreateFeature(feature.Attributes, geometry);
                await newTable.AddFeatureAsync(newFeature);

            }

        }

        public async static Task CoordinateTransformate(this LayerInfo layer, string from, string to)
        {
            if (!CoordinateSystems.Contains(from) || !CoordinateSystems.Contains(to))
            {
                throw new ArgumentException("不能识别坐标系");
            }

            CoordinateTransformation coordinate = new CoordinateTransformation(from, to);

            FeatureQueryResult features = await layer.GetAllFeatures();

            foreach (var feature in features)
            {
                coordinate.Transformate(feature);
                await layer.Table.UpdateFeatureAsync(feature);
            }

        }

        public async static Task SetTimeExtent(this LayerInfo layer)
        {
            if (layer.TimeExtent == null)
            {
                return;
            }
            if (!layer.Table.Fields.Any(p => p.FieldType == FieldType.Date && p.Name == Resource.TimeExtentFieldName))
            {
                throw new Exception("shapefile没有指定的日期属性");
            }
            FeatureLayer  featureLayer = layer.Layer;

            if (layer.TimeExtent.IsEnable)
            {
                List<Feature> visiableFeatures = new List<Feature>();
                List<Feature> invisiableFeatures = new List<Feature>();

                FeatureQueryResult features = await layer.GetAllFeatures();

                foreach (var feature in features)
                {
                    if (feature.Attributes[Resource.TimeExtentFieldName] is DateTimeOffset date)
                    {
                        if (date > layer.TimeExtent.From && date < layer.TimeExtent.To)
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

                featureLayer.SetFeaturesVisible(visiableFeatures, true);
                featureLayer.SetFeaturesVisible(invisiableFeatures, false);
            }
            else
            {
                featureLayer.SetFeaturesVisible(await layer.GetAllFeatures(), true);

            }
        }

        public async static Task<LayerInfo> Union(IEnumerable<LayerInfo> Layers)
        {
            if (Layers == null || !Layers.Any())
            {
                throw new Exception("样式为空");
            }
            var type = Layers.Select(p => p.Type).Distinct();
            if (type.Count() != 1)
            {
                throw new Exception("样式的类型并非统一");
            }
            LayerInfo layer = CreateLayer(type.First());

            foreach (var oldLayer in Layers)
            {
                var oldFeatures = await oldLayer.GetAllFeatures();

                var features = oldFeatures.Select(p => layer.Table.CreateFeature(p.Attributes, p.Geometry));
                await layer.Table.AddFeaturesAsync(features);
                oldLayer.LabelVisible = false;
            }
            layer.UpdateFeatureCount();
            //LayerCollection.Instance.Layers.Add(style);
            return layer;

        }


    }
}