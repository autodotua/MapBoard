using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.IO;
using MapBoard.Common;

using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static MapBoard.Common.CoordinateTransformation;

namespace MapBoard.Main.Util
{
    public static class LayerUtility
    {
        public static string GetFileName(this MapLayerInfo layer)
        {
            return Path.Combine(Config.DataPath, layer.Name + ".shp");
        }

        public static async Task DeleteLayerAsync(this MapLayerInfo layer, MapLayerCollection layers, bool deleteFiles)
        {
            if (layers.Contains(layer))
            {
                layers.Remove(layer);
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

        public async static Task<MapLayerInfo> CreateLayerAsync(GeometryType type,
                                                             MapLayerCollection layers,
                                                             MapLayerInfo template = null,
                                                             string name = null,
                                                             IList<FieldInfo> fields = null)
        {
            if (name == null)
            {
                if (template == null)
                {
                    name = "新图层-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                }
                else
                {
                    name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(template.GetFileName()));
                }
            }
            if (fields == null)
            {
                fields = new List<FieldInfo>();
            }
            await Shapefile.CreateShapefileAsync(type, name, null, fields);
            MapLayerInfo layer = template == null ? new MapLayerInfo() : template.Clone() as MapLayerInfo;
            layer.Fields = fields.ToArray();
            layer.Name = name;
            await layers.AddAsync(layer);
            layers.Selected = layer;
            return layer;
        }

        public async static Task CreatCopyAsync(this MapLayerInfo layer, MapLayerCollection layers, bool includeFeatures)
        {
            if (includeFeatures)
            {
                var features = await layer.GetAllFeaturesAsync();

                var newLayer = await CreateLayerAsync(layer.GeometryType, layers, layer);

                await newLayer.AddFeaturesAsync(features, FeaturesChangedSource.Import);
                layer.LayerVisible = false;
            }
            else
            {
                await CreateLayerAsync(layer.GeometryType, layers, layer);
            }
        }

        public async static Task<Feature[]> GetAllFeaturesAsync(this MapLayerInfo layer)
        {
            FeatureQueryResult result = await layer.QueryFeaturesAsync(new QueryParameters());
            Feature[] array = null;
            await Task.Run(() =>
            {
                array = result.ToArray();
            });
            return array;
        }

        public async static Task CopyAllFeaturesAsync(MapLayerInfo source, MapLayerInfo target)
        {
            var features = await source.GetAllFeaturesAsync();

            await target.AddFeaturesAsync(features, FeaturesChangedSource.FeatureOperation);
        }

        public static async Task LayerCompleteAsync(this MapLayerInfo layer)
        {
            layer.Layer.IsVisible = layer.LayerVisible;
            //layer. Layer.LabelsEnabled = layer.Label == null ? false : layer.Label.Enable;

            await layer.SetTimeExtentAsync();
        }

        public async static Task BufferAsync(this MapLayerInfo layer, MapLayerCollection layers, double meters)
        {
            var template = new MapLayerInfo();
            foreach (var symbol in layer.Symbols)
            {
                template.Symbols.Add(symbol.Key, new SymbolInfo()
                {
                    OutlineWidth = 0,
                    FillColor = symbol.Value.LineColor
                });
            }
            var newLayer = await CreateLayerAsync(GeometryType.Polygon, layers, template, layer.Name + "_缓冲区");
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in await layer.GetAllFeaturesAsync())
            {
                Geometry oldGeometry = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator);
                var geometry = GeometryEngine.Buffer(oldGeometry, meters);
                Feature newFeature = newLayer.CreateFeature(feature.Attributes, geometry);
                newFeatures.Add(newFeature);
            }
            await newLayer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.FeatureOperation);
        }

        public async static Task SetTimeExtentAsync(this MapLayerInfo layer)
        {
            if (layer.TimeExtent == null)
            {
                return;
            }
            //if (!layer.Fields.Any(p => p.Type == FieldInfoType.Date && p.Name == Resource.DateFieldName))
            //{
            //    throw new Exception("shapefile没有指定的日期属性");
            //}
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

        public async static Task CoordinateTransformateAsync(this MapLayerInfo layer, string from, string to)
        {
            if (!CoordinateSystems.Contains(from) || !CoordinateSystems.Contains(to))
            {
                throw new ArgumentException("不能识别坐标系");
            }

            CoordinateTransformation coordinate = new CoordinateTransformation(from, to);

            var features = await layer.GetAllFeaturesAsync();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            foreach (var feature in features)
            {
                newFeatures.Add(new UpdatedFeature(feature));
                coordinate.Transformate(feature);
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeaturesChangedSource.FeatureOperation);
        }

        public async static Task<MapLayerInfo> UnionAsync(IEnumerable<MapLayerInfo> layers, MapLayerCollection layerCollection)
        {
            if (layers == null || !layers.Any())
            {
                throw new Exception("图层为空");
            }
            var type = layers.Select(p => p.GeometryType).Distinct();
            if (type.Count() != 1)
            {
                throw new Exception("图层的类型并非统一");
            }
            MapLayerInfo layer = await CreateLayerAsync(type.First(), layerCollection);

            foreach (var oldLayer in layers)
            {
                var oldFeatures = await oldLayer.GetAllFeaturesAsync();

                var features = oldFeatures.Select(p => layer.CreateFeature(p.Attributes, p.Geometry));
                await layer.AddFeaturesAsync(features, FeaturesChangedSource.FeatureOperation);
                oldLayer.LayerVisible = false;
            }
            return layer;
        }
    }
}