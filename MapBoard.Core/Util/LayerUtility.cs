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

namespace MapBoard.Util
{
    public static class LayerUtility
    {
        public static string GetFilePath(this MapLayerInfo layer)
        {
            return Path.Combine(Parameters.DataPath, layer.Name + ".shp");
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
                    name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(template.GetFilePath()));
                }
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

        public static FeatureQueryResult GetAllFeatures(this MapLayerInfo layer)
        {
            return layer.QueryFeaturesAsync(new QueryParameters()).Result;
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
            if (layer.TimeExtent != null && layer.TimeExtent.IsEnable)
            {
                await layer.SetTimeExtentAsync();
            }
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
        public async static Task SetTimeExtentAsync(this MapLayerInfo layer)
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

        public async static Task CoordinateTransformateAsync(this MapLayerInfo layer, CoordinateSystem source, CoordinateSystem target)
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

        public async static Task<MapLayerInfo> UnionAsync(IEnumerable<MapLayerInfo> layers, MapLayerCollection layerCollection)
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
            MapLayerInfo layer = await CreateLayerAsync(type.First(), layerCollection);
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