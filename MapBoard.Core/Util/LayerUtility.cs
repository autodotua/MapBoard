using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib;
using FzLib.IO;
using MapBoard.IO;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Util
{
    public static class LayerUtility
    {
        /// <summary>
        /// 添加WFS图层
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <param name="layerName"></param>
        /// <param name="autoPopulateAll"></param>
        /// <returns></returns>
        public static async Task<WfsMapLayerInfo> AddWfsLayerAsync(MapLayerCollection layers, string name, string url, string layerName, bool autoPopulateAll)
        {
            WfsMapLayerInfo layer = new WfsMapLayerInfo(name, url, layerName, autoPopulateAll);
            await layers.AddAsync(layer);
            return layer;
        }

        /// <summary>
        /// 建立缓冲区
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layers"></param>
        /// <param name="targetLayer"></param>
        /// <param name="meters"></param>
        /// <param name="union"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static async Task BufferAsync(this IMapLayerInfo layer, MapLayerCollection layers, IEditableLayerInfo targetLayer, double[] meters, bool union, Feature[] features = null)
        {
            if (targetLayer == null)
            {
                var template = EmptyMapLayerInfo.CreateTemplate();
                foreach (var symbol in layer.Renderer.Symbols)
                {
                    template.Renderer.Symbols.Add(symbol.Key, new SymbolInfo()
                    {
                        OutlineWidth = 0,
                        FillColor = symbol.Value.LineColor
                    });
                }
                template.Fields = new FieldInfo[]
                {
                    new FieldInfo("RingIndex","环索引",FieldInfoType.Integer)
                };
                targetLayer = await CreateShapefileLayerAsync(GeometryType.Polygon, layers, template, true, layer.Name + "-缓冲区");
            }
            await FeatureUtility.BufferToLayerAsync(layer, targetLayer, features == null ? await layer.GetAllFeaturesAsync() : features, meters, union);
        }

        /// <summary>
        /// 复制所有要素
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static async Task CopyAllFeaturesAsync(IMapLayerInfo source, ShapefileMapLayerInfo target)
        {
            var features = await source.GetAllFeaturesAsync();

            await target.AddFeaturesAsync(features, FeaturesChangedSource.FeatureOperation);
        }

        /// <summary>
        /// 复制样式
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void CopyStyles(IMapLayerInfo source, IMapLayerInfo target)
        {
            target.Labels = source.Labels.Select(p => p.Clone() as LabelInfo).ToArray();
            target.Renderer = source.Renderer.Clone() as UniqueValueRendererInfo;
        }

        /// <summary>
        /// 创建图层的副本
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layers"></param>
        /// <param name="includeFeatures"></param>
        /// <param name="includeFields"></param>
        /// <returns></returns>
        public static async Task CreateCopyAsync(this IMapLayerInfo layer, MapLayerCollection layers, bool includeFeatures, bool includeFields)
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


        /// <summary>
        /// 创建Shapefile图层
        /// </summary>
        /// <param name="type"></param>
        /// <param name="layers"></param>
        /// <param name="name"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static Task<ShapefileMapLayerInfo> CreateShapefileLayerAsync(GeometryType type,
                                                             MapLayerCollection layers,
                                                             string name = null,
                                                             IList<FieldInfo> fields = null)
        {
            return CreateShapefileLayerAsync(type, layers, null, false, fields, name);
        }

        /// <summary>
        /// 从模板创建Shapefile图层
        /// </summary>
        /// <param name="type"></param>
        /// <param name="layers"></param>
        /// <param name="template"></param>
        /// <param name="includeFields"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Task<ShapefileMapLayerInfo> CreateShapefileLayerAsync(GeometryType type,
                                                             MapLayerCollection layers,
                                                             IMapLayerInfo template,
                                                             bool includeFields,
                                                             string name = null)
        {
            return CreateShapefileLayerAsync(type, layers, template, includeFields, null, name);
        }

        /// <summary>
        /// 通过模板和指定字段创建Shapefile图层
        /// </summary>
        /// <param name="type"></param>
        /// <param name="layers"></param>
        /// <param name="template"></param>
        /// <param name="importTemplateFields"></param>
        /// <param name="fields"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<ShapefileMapLayerInfo> CreateShapefileLayerAsync(GeometryType type,
                                                             MapLayerCollection layers,
                                                             IMapLayerInfo template,
                                                             bool importTemplateFields,
                                                             IEnumerable<FieldInfo> fields,
                                                             string name = null)
        {
            if (fields != null && importTemplateFields)
            {
                throw new ArgumentException("不可同时继承字段又指定字段");
            }

            //处理图层名
            if (name == null)
            {
                name = template == null ?
                    "新图层-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")
                    : template.Name;
            }
            name = Path.GetFileNameWithoutExtension(FileSystem.GetNoDuplicateFile(Path.Combine(FolderPaths.DataPath, name + ".shp")));

            //处理字段
            if (importTemplateFields)
            {
                fields = template.Fields;
            }
            else
            {
                fields ??= new List<FieldInfo>();
            }

            //创建图层
            await Shapefile.CreateShapefileAsync(type, name, null, fields);
            Debug.Assert(template is null or MapLayerInfo);
            ShapefileMapLayerInfo layer = template == null ?
                new ShapefileMapLayerInfo(name)
                : new ShapefileMapLayerInfo(template as MapLayerInfo, name, importTemplateFields);

            layer.Fields = fields.ToArray();
            if (layers != null)
            {
                await layers.AddAsync(layer);
                layer.LayerVisible = true;
                layers.Selected = layer;
            }
            return layer;
        }

        /// <summary>
        /// 创建临时图层
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static async Task<TempMapLayerInfo> CreateTempLayerAsync(MapLayerCollection layers, string name, GeometryType type, IList<FieldInfo> fields = null)
        {
            TempMapLayerInfo layer = new TempMapLayerInfo(name, type, fields);
            await layers.AddAsync(layer);
            return layer;
        }

        /// <summary>
        /// 删除图层
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layers"></param>
        /// <param name="deleteFiles"></param>
        /// <returns></returns>
        public static async Task DeleteLayerAsync(this MapLayerInfo layer, MapLayerCollection layers, bool deleteFiles)
        {
            if (layers != null && layers.Contains(layer))
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

        public static async Task<ShapefileMapLayerInfo> ExportLayerAsync(IMapLayerInfo oldLayer, MapLayerCollection layers, string name, IEnumerable<ExportingFieldInfo> fields)
        {
            var fieldList = fields.Where(p => p.Enable).ToList();
            if (fieldList.Count != fieldList.Select(p => p.Name).Distinct().Count())
            {
                throw new Exception("存在重复的字段名");
            }
            var layer = await CreateShapefileLayerAsync(oldLayer.GeometryType, layers, oldLayer, false, fieldList, name);
            Dictionary<string, string> oldName2newName = new Dictionary<string, string>();
            foreach (var field in fields)
            {
                if (field.OldField != null)
                {
                    oldName2newName.Add(field.OldField.Name, field.Name);
                }
            }

            var oldFeatures = await oldLayer.GetAllFeaturesAsync();
            List<Feature> newFeatures = new List<Feature>(oldFeatures.Length);
            foreach (var feature in oldFeatures)
            {
                Dictionary<string, object> newAttributes = new Dictionary<string, object>();
                foreach (var attribute in feature.Attributes)
                {
                    if (oldName2newName.ContainsKey(attribute.Key))
                    {
                        newAttributes.Add(oldName2newName[attribute.Key], attribute.Value);
                    }
                }

                newFeatures.Add(layer.CreateFeature(newAttributes, feature.Geometry));
            }

            await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.Import);

            return layer;
        }

        /// <summary>
        /// 根据Esri图层，寻找MapBoard图层
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// 获取所有要素
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static FeatureQueryResult GetAllFeatures(this IMapLayerInfo layer)
        {
            return layer.QueryFeaturesAsync(new QueryParameters()).Result;
        }

        /// <summary>
        /// 获取所有要素
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取图层中要素属性值的唯一值
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static async Task<object[]> GetUniqueAttributeValues(this IMapLayerInfo layer, string fieldName)
        {
            var parameters = new StatisticsQueryParameters(new StatisticDefinition[]
            {
                new StatisticDefinition(fieldName,StatisticType.Count,null)
            });
            parameters.GroupByFieldNames.Add(fieldName);
            var result = await layer.Layer.FeatureTable.QueryStatisticsAsync(parameters);
            return result.Select(p => p.Group[fieldName]).ToArray();
        }

        /// <summary>
        /// 从Feature导入到地图图层
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="layers"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static async Task<ShapefileMapLayerInfo> ImportFromFeatureTable(string layerName, MapLayerCollection layers, FeatureTable table)
        {
            await table.LoadAsync();
            FeatureQueryResult features = await table.QueryFeaturesAsync(new QueryParameters());
            var fieldMap = table.Fields.FromEsriFields();//从原表字段名到新字段的映射
            ShapefileMapLayerInfo layer = await CreateShapefileLayerAsync(table.GeometryType, layers,
                 layerName, fieldMap.Values.ToList());
            layer.LayerVisible = false;
            var fields = layer.Fields.Select(p => p.Name).ToHashSet();
            List<Feature> newFeatures = new List<Feature>();
            await Task.Run(() =>
            {
                foreach (var feature in features)
                {
                    Dictionary<string, object> newAttributes = new Dictionary<string, object>();
                    foreach (var attr in feature.Attributes)
                    {
                        if (attr.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        string name = attr.Key;//现在是源文件的字段名

                        if (!fieldMap.ContainsKey(name))
                        {
                            continue;
                        }
                        name = fieldMap[name].Name;//切换到目标表的字段名

                        object value = attr.Value;
                        if (value is short)
                        {
                            value = Convert.ToInt32(value);
                        }
                        else if (value is float)
                        {
                            value = Convert.ToDouble(value);
                        }
                        newAttributes.Add(name, value);
                    }
                    Feature newFeature = layer.CreateFeature(newAttributes, feature.Geometry.RemoveZAndM());
                    newFeatures.Add(newFeature);
                }
            });
            await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.Import);

            layer.LayerVisible = true;
            return layer;
        }
        /// <summary>
        /// 建立缓冲区
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="layers"></param>
        /// <param name="meters"></param>
        /// <returns></returns>

        [Obsolete]
        public static async Task SimpleBufferAsync(this IMapLayerInfo layer, MapLayerCollection layers, double meters)
        {
            var template = EmptyMapLayerInfo.CreateTemplate();
            foreach (var symbol in layer.Renderer.Symbols)
            {
                template.Renderer.Symbols.Add(symbol.Key, new SymbolInfo()
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
        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="layerCollection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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
    }
}