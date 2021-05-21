using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Labeling;
using Esri.ArcGISRuntime.Symbology;
using FzLib.IO;
using FzLib.UI.Dialog;
using MapBoard.Common;

using MapBoard.Common;

using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MapBoard.Common.CoordinateTransformation;
using LayerCollection = MapBoard.Main.Model.LayerCollection;

namespace MapBoard.Main.Util
{
    public static class LayerUtility
    {
        public static async Task RemoveLayerAsync(this LayerInfo layer, bool deleteFiles)
        {
            if (LayerCollection.Instance.Contains(layer))
            {
                LayerCollection.Instance.Remove(layer);
            }

            if (deleteFiles)
            {
                await Task.Run(() =>
                {
                    foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath, layer.Name))
                    {
                        if (Path.GetFileNameWithoutExtension(file) == layer.Name)
                        {
                            File.Delete(file);
                        }
                    }
                });
            }
        }

        public async static Task<LayerInfo> CreateLayerAsync(GeometryType type, LayerInfo template = null, string name = null, IList<FieldInfo> fields = null)
        {
            if (name == null)
            {
                if (template == null)
                {
                    name = "新图层-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                }
                else
                {
                    name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(template.FileName));
                }
            }
            if (fields == null)
            {
                fields = new List<FieldInfo>();
            }
            await Shapefile.CreateShapefileAsync(type, name, null, fields);
            LayerInfo layer = new LayerInfo();
            layer.Fields = fields.ToArray();
            if (template != null)
            {
                layer.CopyLayerFrom(template);
            }
            layer.Name = name;
            await LayerCollection.Instance.AddAsync(layer);
            LayerCollection.Instance.Selected = layer;
            return layer;
        }

        public async static Task CreatCopyAsync(this LayerInfo layer, bool includeFeatures)
        {
            if (includeFeatures)
            {
                var features = await LayerCollection.Instance.Selected.GetAllFeaturesAsync();

                var newLayer = await CreateLayerAsync(layer.Type, layer);
                ShapefileFeatureTable targetTable = newLayer.Table;

                await targetTable.AddFeaturesAsync(features);
                newLayer.UpdateFeatureCount();
                layer.LayerVisible = false;
            }
            else
            {
                await CreateLayerAsync(layer.Type, layer);
            }
        }

        public async static Task<Feature[]> GetAllFeaturesAsync(this LayerInfo layer)
        {
            FeatureQueryResult result = await layer.Table.QueryFeaturesAsync(new QueryParameters());
            Feature[] array = null;
            await Task.Run(() =>
            {
                array = result.ToArray();
            });
            return array;
        }

        public async static Task CopyAllFeaturesAsync(LayerInfo source, LayerInfo target)
        {
            var features = await source.GetAllFeaturesAsync();
            ShapefileFeatureTable targetTable = target.Table;

            foreach (var feature in features)
            {
                await targetTable.AddFeatureAsync(feature);
            }
            target.UpdateFeatureCount();
        }

        public static async Task LayerCompleteAsync(this LayerInfo layer)
        {
            layer.Layer.IsVisible = layer.LayerVisible;
            //layer. Layer.LabelsEnabled = layer.Label == null ? false : layer.Label.Enable;

            await layer.SetTimeExtentAsync();
        }

        public async static Task BufferAsync(this LayerInfo layer)
        {
            var newLayer = await CreateLayerAsync(GeometryType.Polygon, layer);

            ShapefileFeatureTable newTable = newLayer.Table;

            foreach (var feature in await layer.GetAllFeaturesAsync())
            {
                Geometry oldGeometry = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator);
                var geometry = GeometryEngine.Buffer(oldGeometry, Config.Instance.StaticWidth);
                Feature newFeature = newTable.CreateFeature(feature.Attributes, geometry);
                await newTable.AddFeatureAsync(newFeature);
            }
        }

        public async static Task SetTimeExtentAsync(this LayerInfo layer)
        {
            if (layer.TimeExtent == null)
            {
                return;
            }
            if (!layer.Table.Fields.Any(p => p.FieldType == FieldType.Date && p.Name == Resource.DateFieldName))
            {
                throw new Exception("shapefile没有指定的日期属性");
            }
            FeatureLayer featureLayer = layer.Layer;

            if (layer.TimeExtent.IsEnable)
            {
                List<Feature> visiableFeatures = new List<Feature>();
                List<Feature> invisiableFeatures = new List<Feature>();

                var features = await layer.GetAllFeaturesAsync();

                foreach (var feature in features)
                {
                    if (feature.Attributes[Resource.DateFieldName] is DateTimeOffset date)
                    {
                        if (date.UtcDateTime >= layer.TimeExtent.From && date.UtcDateTime <= layer.TimeExtent.To)
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
                featureLayer.SetFeaturesVisible(await layer.GetAllFeaturesAsync(), true);
            }
        }

        public async static Task CoordinateTransformateAsync(this LayerInfo layer, string from, string to)
        {
            if (!CoordinateSystems.Contains(from) || !CoordinateSystems.Contains(to))
            {
                throw new ArgumentException("不能识别坐标系");
            }

            CoordinateTransformation coordinate = new CoordinateTransformation(from, to);

            var features = await layer.GetAllFeaturesAsync();

            foreach (var feature in features)
            {
                coordinate.Transformate(feature);
                await layer.Table.UpdateFeatureAsync(feature);
            }
        }

        public async static Task<LayerInfo> UnionAsync(IEnumerable<LayerInfo> layers)
        {
            if (layers == null || !layers.Any())
            {
                throw new Exception("图层为空");
            }
            var type = layers.Select(p => p.Type).Distinct();
            if (type.Count() != 1)
            {
                throw new Exception("图层的类型并非统一");
            }
            LayerInfo layer = await CreateLayerAsync(type.First());

            foreach (var oldLayer in layers)
            {
                var oldFeatures = await oldLayer.GetAllFeaturesAsync();

                var features = oldFeatures.Select(p => layer.Table.CreateFeature(p.Attributes, p.Geometry));
                await layer.Table.AddFeaturesAsync(features);
                oldLayer.LayerVisible = false;
            }
            layer.UpdateFeatureCount();
            //LayerCollection.Instance.Add(style);
            return layer;
        }
    }
}