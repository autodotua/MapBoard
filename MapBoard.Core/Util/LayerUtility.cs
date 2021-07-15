using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.IO;

using MapBoard.IO;
using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static MapBoard.Util.CoordinateTransformation;
using MapBoard.Mapping.Model;
using FzLib.Basic;
using System.Diagnostics;

namespace MapBoard.Util
{
    public static class LayerUtility
    {
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
                    foreach (var file in Shapefile.GetExistShapefiles(Parameters.DataPath, layer.Name))
                    {
                        if (Path.GetFileNameWithoutExtension(file) == layer.Name)
                        {
                            File.Delete(file);
                        }
                    }
                });
            }
        }

        public async static Task<ShapefileMapLayerInfo> CreateLayerAsync(GeometryType type,
                                                             MapLayerCollection layers,
                                                             string name = null,
                                                             IList<FieldInfo> fields = null)
        {
            if (name == null)
            {
                name = "新图层-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            }
            else
            {
                name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(Path.Combine(Parameters.DataPath, name + ".shp")));
            }
            if (fields == null)
            {
                fields = new List<FieldInfo>();
            }
            await Shapefile.CreateShapefileAsync(type, name, null, fields);
            ShapefileMapLayerInfo layer = new ShapefileMapLayerInfo(name);
            layer.Fields = fields.ToArray();
            await layers.AddAsync(layer);
            layers.Selected = layer;
            return layer;
        }

        public async static Task<ShapefileMapLayerInfo> CreateLayerAsync(GeometryType type,
                                                             MapLayerCollection layers,
                                                             IMapLayerInfo template,
                                                             bool includeFields,
                                                             string name = null)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
            if (name == null)
            {
                name = template.Name;
            }
            name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(Path.Combine(Parameters.DataPath, name + ".shp")));

            await Shapefile.CreateShapefileAsync(type, name, null, template.Fields);
            Debug.Assert(template is MapLayerInfo);
            ShapefileMapLayerInfo layer = template == null ? new ShapefileMapLayerInfo(name) : new ShapefileMapLayerInfo(template as MapLayerInfo, name, includeFields);
            await layers.AddAsync(layer);
            layers.Selected = layer;
            return layer;
        }

        public async static Task CreatCopyAsync(this IMapLayerInfo layer, MapLayerCollection layers, bool includeFeatures, bool includeFields)
        {
            if (includeFeatures)
            {
                var features = await layer.GetAllFeaturesAsync();

                var newLayer = await CreateLayerAsync(layer.GeometryType, layers, layer, includeFields);

                await newLayer.AddFeaturesAsync(features, FeaturesChangedSource.Import);
                layer.LayerVisible = false;
            }
            else
            {
                await CreateLayerAsync(layer.GeometryType, layers, layer, includeFields);
            }
        }

        public async static Task<Feature[]> GetAllFeaturesAsync(this IMapLayerInfo layer)
        {
            FeatureQueryResult result = await layer.QueryFeaturesAsync(new QueryParameters());
            Feature[] array = null;
            await Task.Run(() =>
            {
                array = result.ToArray();
            });
            return array;
        }

        public static FeatureQueryResult GetAllFeatures(this IMapLayerInfo layer)
        {
            return layer.QueryFeaturesAsync(new QueryParameters()).Result;
        }

        public async static Task CopyAllFeaturesAsync(IMapLayerInfo source, ShapefileMapLayerInfo target)
        {
            var features = await source.GetAllFeaturesAsync();

            await target.AddFeaturesAsync(features, FeaturesChangedSource.FeatureOperation);
        }

        public static async Task LayerCompleteAsync(this IMapLayerInfo layer)
        {
            layer.Layer.IsVisible = layer.LayerVisible;
            //layer. Layer.LabelsEnabled = layer.Label == null ? false : layer.Label.Enable;
            if (layer.TimeExtent != null && layer.TimeExtent.IsEnable)
            {
                await layer.SetTimeExtentAsync();
            }
        }

        public async static Task BufferAsync(this IMapLayerInfo layer, MapLayerCollection layers, double meters)
        {
            var template = EmptyMapLayerInfo.CreateTemplate();
            foreach (var symbol in layer.Symbols)
            {
                template.Symbols.Add(symbol.Key, new SymbolInfo()
                {
                    OutlineWidth = 0,
                    FillColor = symbol.Value.LineColor
                });
            }
            var newLayer = await CreateLayerAsync(GeometryType.Polygon, layers, template, true, layer.Name + "-缓冲区");
            List<Feature> newFeatures = new List<Feature>();
            await Task.Run(() =>
            {
                foreach (var feature in layer.GetAllFeatures())
                {
                    Geometry oldGeometry = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator);
                    var geometry = GeometryEngine.Buffer(oldGeometry, meters);
                    Feature newFeature = newLayer.CreateFeature(feature.Attributes, geometry);
                    newFeatures.Add(newFeature);
                }
            });
            await newLayer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.FeatureOperation);
        }

        /// <summary>
        /// 根据时间范围，控制每个要素的可见性。
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public async static Task SetTimeExtentAsync(this IMapLayerInfo layer)
        {
            if (layer.TimeExtent == null)
            {
                return;
            }
            FeatureLayer featureLayer = layer.Layer;

            if (layer.TimeExtent.IsEnable)
            {
                List<Feature> visiableFeatures = new List<Feature>();
                List<Feature> invisiableFeatures = new List<Feature>();

                var features = await layer.GetAllFeaturesAsync();

                foreach (var feature in features)
                {
                    if (feature.Attributes[Parameters.DateFieldName] is DateTimeOffset date)
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

        public async static Task CoordinateTransformateAsync(this IWriteableLayerInfo layer, CoordinateSystem source, CoordinateSystem target)
        {
            if (source == target)
            {
                return;
            }
            var features = await layer.GetAllFeaturesAsync();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            foreach (var feature in features)
            {
                newFeatures.Add(new UpdatedFeature(feature));
                feature.Geometry = Transformate(feature.Geometry, source, target);
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeaturesChangedSource.FeatureOperation);
        }

        public async static Task<ShapefileMapLayerInfo> UnionAsync(IEnumerable<MapLayerInfo> layers, MapLayerCollection layerCollection)
        {
            if (layers == null || !layers.Any())
            {
                throw new ArgumentException("图层为空");
            }
            var type = layers.Select(p => p.GeometryType).Distinct();
            if (type.Count() != 1)
            {
                throw new ArgumentException("图层的类型并非统一");
            }
            var layer = await CreateLayerAsync(type.First(), layerCollection);
            List<Feature> newFeatures = new List<Feature>();
            await Task.Run(() =>
            {
                foreach (var oldLayer in layers)
                {
                    var oldFeatures = oldLayer.GetAllFeatures();
                    var features = oldFeatures.Select(p => layer.CreateFeature(p.Attributes, p.Geometry));
                    newFeatures.AddRange(features);
                }
            });
            await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.FeatureOperation);
            layers.ForEach(p => p.LayerVisible = false);

            return layer;
        }
    }
}