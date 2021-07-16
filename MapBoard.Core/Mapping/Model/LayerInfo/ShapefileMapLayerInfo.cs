using AutoMapper;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic.Collection;
using FzLib.Extension;
using MapBoard.IO;
using MapBoard.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public class ShapefileMapLayerInfo : MapLayerInfo, IEditableLayerInfo, IHasDefaultFields
    {
        public ShapefileMapLayerInfo() : base()
        {
        }

        public ShapefileMapLayerInfo(ILayerInfo layer) : base(layer)
        {
        }

        public ShapefileMapLayerInfo(string name) : base(name)
        {
        }

        public ShapefileMapLayerInfo(MapLayerInfo template, string newName, bool includeFields)
        {
            new MapperConfiguration(cfg =>
               {
                   cfg.CreateMap<LayerInfo, ShapefileMapLayerInfo>();
               }).CreateMapper().Map<LayerInfo, ShapefileMapLayerInfo>(template, this);
            Name = newName;

            if (!includeFields)
            {
                Fields = Array.Empty<FieldInfo>();
            }
        }

        public string FilePath => Path.Combine(Parameters.DataPath, Name + ".shp");

        public override object Clone()
        {
            var layer = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<LayerInfo, ShapefileMapLayerInfo>();
            }).CreateMapper().Map<ShapefileMapLayerInfo>(this);
            return layer;
        }

        public override string Type => Types.Shapefile;
        public override bool IsEditable => true;

        public event EventHandler<FeaturesChangedEventArgs> FeaturesChanged;

        private void NotifyFeaturesChanged(IEnumerable<Feature> added,
            IEnumerable<Feature> deleted,
            IEnumerable<UpdatedFeature> updated,
            FeaturesChangedSource source)
        {
            this.Notify(nameof(NumberOfFeatures));
            var h = new FeaturesChangedEventArgs(this, added, deleted, updated, source);
            FeaturesChanged?.Invoke(this, h);
            Histories.Add(h);
        }

        [JsonIgnore]
        [IgnoreMap]
        public ObservableCollection<FeaturesChangedEventArgs> Histories { get; private set; } = new ObservableCollection<FeaturesChangedEventArgs>();

        public async Task AddFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            if (!feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName)
                || feature.Attributes[Parameters.CreateTimeFieldName] == null)
            {
                feature.SetAttributeValue(Parameters.CreateTimeFieldName, DateTime.Now.ToString(Parameters.TimeFormat));
            }
            await table.AddFeatureAsync(feature);

            NotifyFeaturesChanged(new[] { feature }, null, null, source);
        }

        /// <summary>
        /// 增加一些要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <param name="rebuildFeature">是否需要根据属性和图形新建要素。若源要素属于另一个拥有不同字段的要素类，则该值应设为True</param>
        /// <returns></returns>
        public async Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source, bool rebuildFeature = false)
        {
            //记录含有Int64的字段
            List<string> Int64Fields = null;
            if (rebuildFeature)
            {
                List<Feature> newFeatures = new List<Feature>();
                foreach (var feature in features)
                {
                    //仅查询一次Int64字段
                    if (Int64Fields == null)
                    {
                        Int64Fields = feature.Attributes.Where(p => p.Value is Int64).Select(p => p.Key).ToList();
                    }

                    var dic = new Dictionary<string, object>(feature.Attributes);
                    //若存在Int64，那么需要转换为Int32。因为Shapefile不支持Int64
                    if (Int64Fields.Count > 0)
                    {
                        foreach (var f in Int64Fields)
                        {
                            long value = (long)dic[f];
                            //超过范围的，直接置0
                            if (value > int.MaxValue || value < int.MinValue)
                            {
                                dic[f] = 0;
                            }
                            else
                            {
                                dic[f] = Convert.ToInt32(value);
                            }
                        }
                    }
                    if (!feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName)
                        || feature.Attributes[Parameters.CreateTimeFieldName] == null)
                    {
                        dic.AddOrSetValue(Parameters.CreateTimeFieldName, DateTime.Now.ToString(Parameters.TimeFormat));
                    }
                    var newFeature = CreateFeature(dic, feature.Geometry);
                    newFeatures.Add(newFeature);
                }
                await table.AddFeaturesAsync(newFeatures);
                NotifyFeaturesChanged(newFeatures, null, null, source);
            }
            else
            {
                foreach (var feature in features)
                {
                    if (!feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName)
                        || feature.Attributes[Parameters.CreateTimeFieldName] == null)
                    {
                        feature.SetAttributeValue(Parameters.CreateTimeFieldName, DateTime.Now.ToString(Parameters.TimeFormat));
                    }
                }
                await table.AddFeaturesAsync(features);
                NotifyFeaturesChanged(features, null, null, source);
            }
        }

        public async Task DeleteFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            await table.DeleteFeatureAsync(feature);
            NotifyFeaturesChanged(null, new[] { feature }, null, source);
        }

        public async Task DeleteFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
        {
            await table.DeleteFeaturesAsync(features);
            NotifyFeaturesChanged(null, features, null, source);
        }

        public async Task UpdateFeatureAsync(UpdatedFeature feature, FeaturesChangedSource source)
        {
            await table.UpdateFeatureAsync(feature.Feature);
            NotifyFeaturesChanged(null, null, new[] { feature }, source);
        }

        public async Task UpdateFeaturesAsync(IEnumerable<UpdatedFeature> features, FeaturesChangedSource source)
        {
            await table.UpdateFeaturesAsync(features.Select(p => p.Feature));
            NotifyFeaturesChanged(null, null, features, source);
        }

        public Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry)
        {
            return table.CreateFeature(attributes, geometry);
        }

        public Feature CreateFeature()
        {
            return table.CreateFeature();
        }

        protected override FeatureTable GetTable()
        {
            return new ShapefileFeatureTable(FilePath);
        }

        public override void Dispose()
        {
            (table as ShapefileFeatureTable)?.Close();
            table = null;
            base.Dispose();
        }

        public async override Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            Debug.Assert(!string.IsNullOrEmpty(newName));
            if (newName == Name)
            {
                return;
            }
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                   || newName.Length > 240 || newName.Length < 1)
            {
                throw new IOException("新文件名不合法");
            }
            //检查文件存在
            foreach (var file in Shapefile.GetExistShapefiles(Parameters.DataPath, Layer.Name))
            {
                if (File.Exists(Path.Combine(Parameters.DataPath, newName + Path.GetExtension(file))))
                {
                    throw new IOException("该名称的文件已存在");
                }
            }
            //检查图层是否在集合中
            if (!layers.Contains(Layer))
            {
                throw new ArgumentException("本图层不在给定的图层集合中");
            }
            int index = layers.IndexOf(Layer);

            (table as ShapefileFeatureTable).Close();
            //重命名
            foreach (var file in Shapefile.GetExistShapefiles(Parameters.DataPath, Layer.Name))
            {
                File.Move(file, Path.Combine(Parameters.DataPath, newName + Path.GetExtension(file)));
            }
            Name = newName;
            await LoadAsync();
            layers[index] = Layer;
        }
    }
}