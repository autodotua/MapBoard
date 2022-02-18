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
using System.Diagnostics;
using FzLib;

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
                if (layer is ShapefileMapLayerInfo)
                {
                    await Task.Run(() =>
                    {
                        foreach (var file in Shapefile.GetExistShapefiles(FolderPaths.DataPath, layer.Name))
                        {
                            File.Delete(file);
                        }
                    });
                }
            }
        }

        public static async Task<WfsMapLayerInfo> AddWfsLayerAsync(MapLayerCollection layers, string name, string url, string layerName, bool autoPopulateAll)
        {
            WfsMapLayerInfo layer = new WfsMapLayerInfo(name, url, layerName, autoPopulateAll);
            await layers.AddAsync(layer);
            return layer;
        }

        public static async Task<TempMapLayerInfo> CreateTempLayerAsync(MapLayerCollection layers, string name, GeometryType type, IList<FieldInfo> fields = null)
        {
            TempMapLayerInfo layer = new TempMapLayerInfo(name, type, fields);
            await layers.AddAsync(layer);
            return layer;
        }

        public static async Task<ShapefileMapLayerInfo> CreateShapefileLayerAsync(GeometryType type,
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
                name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(Path.Combine(FolderPaths.DataPath, name + ".shp")));
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

        public static async Task<ShapefileMapLayerInfo> CreateShapefileLayerAsync(GeometryType type,
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
            name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(Path.Combine(FolderPaths.DataPath, name + ".shp")));

            await Shapefile.CreateShapefileAsync(type, name, null, template.Fields);
            Debug.Assert(template is MapLayerInfo);
            ShapefileMapLayerInfo layer = new ShapefileMapLayerInfo(template as MapLayerInfo, name, includeFields);
            await layers.AddAsync(layer);
            layers.Selected = layer;
            return layer;
        }

        public static async Task CreatCopyAsync(this IMapLayerInfo layer, MapLayerCollection layers, bool includeFeatures, bool includeFields)
        {
            if (includeFeatures)
            {
                var features = await layer.GetAllFeaturesAsync();

                var newLayer = await CreateShapefileLayerAsync(layer.GeometryType, layers, layer, includeFields);

                await newLayer.AddFeaturesAsync(features, FeaturesChangedSource.Import, true);
                layer.LayerVisible = false;
            }
            else
            {
                await CreateShapefileLayerAsync(layer.GeometryType, layers, layer, includeFields);
            }
        }

        public static async Task<Feature[]> GetAllFeaturesAsync(this IMapLayerInfo layer)
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

        public static async Task CopyAllFeaturesAsync(IMapLayerInfo source, ShapefileMapLayerInfo target)
        {
            var features = await source.GetAllFeaturesAsync();

            await target.AddFeaturesAsync(features, FeaturesChangedSource.FeatureOperation);
        }

        public static async Task LayerCompleteAsync(this IMapLayerInfo layer)
        {
            layer.Layer.IsVisible = layer.LayerVisible;
            //layer. Layer.LabelsEnabled = layer.Label == null ? false : layer.Label.Enable;
            if (layer is IHasDefaultFields && layer.TimeExtent?.IsEnable == true)
            {
                await layer.SetTimeExtentAsync();
            }
        }

        /// <summary>
        /// 建立缓冲区
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layers"></param>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static async Task SimpleBufferAsync(this IMapLayerInfo layer, MapLayerCollection layers, double meters)
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
            var newLayer = await CreateShapefileLayerAsync(GeometryType.Polygon, layers, template, true, layer.Name + "-缓冲区");
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

        public static async Task BufferAsync(this IMapLayerInfo layer, MapLayerCollection layers, IEditableLayerInfo targetLayer, double meters, bool union, Feature[] features = null)
        {
            if (targetLayer == null)
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
                targetLayer = await CreateShapefileLayerAsync(GeometryType.Polygon, layers, template, true, layer.Name + "-缓冲区");
            }
            await FeatureUtility.BufferToLayerAsync(layer, targetLayer, features == null ? await layer.GetAllFeaturesAsync() : features, meters, union);
        }

        /// <summary>
        /// 根据时间范围，控制每个要素的可见性。
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static async Task SetTimeExtentAsync(this IMapLayerInfo layer)
        {
            if (layer.TimeExtent == null || !(layer is IHasDefaultFields))
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

        public static async Task CoordinateTransformateAsync(this IEditableLayerInfo layer, CoordinateSystem source, CoordinateSystem target)
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

        public static async Task<ShapefileMapLayerInfo> UnionAsync(IEnumerable<MapLayerInfo> layers, MapLayerCollection layerCollection)
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
            var layer = await CreateShapefileLayerAsync(type.First(), layerCollection);
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

        public static MapLayerInfo FindLayer(this MapLayerCollection layers, ILayerContent layer)
        {
            if (layer is FeatureLayer l)
            {
                return layers.Find(l);
            }
            else if (layer is FeatureCollectionLayer cl)
            {
                return layers.Find(cl.Layers[0]);
            }
            throw new Exception("找不到指定的图层");
        }
    }
}