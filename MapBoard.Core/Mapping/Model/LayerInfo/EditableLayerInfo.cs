using AutoMapper;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib;
using FzLib.Collection;
using MapBoard.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public abstract class EditableLayerInfo : MapLayerInfo, IEditableLayerInfo
    {
        public EditableLayerInfo() : base()
        {
        }

        public EditableLayerInfo(string name) : base(name)
        {
        }

        public EditableLayerInfo(ILayerInfo layer) : base(layer)
        {
        }

        private void ThrowIfNotEditable()
        {
            if (!Interaction.CanEdit)
            {
                throw new NotSupportedException("当前图层被禁止编辑");
            }
        }

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

        public Task AddFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            return AddFeatureAsync(feature, source, feature.FeatureTable != table);
        }

        public async Task AddFeatureAsync(Feature feature, FeaturesChangedSource source, bool rebuildFeature)
        {
            ThrowIfNotEditable();
            Feature newFeature = rebuildFeature ? feature.Clone(this) : feature;
            await table.AddFeatureAsync(newFeature);
            NotifyFeaturesChanged(new[] { feature }, null, null, source);
        }

        public Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            if (features.Select(p => p.FeatureTable).Distinct().Count() != 1)
            {
                throw new ArgumentException("集合为空或要素来自不同的要素类");
            }
            return AddFeaturesAsync(features, source, features.First().FeatureTable != table);
        }

        /// <summary>
        /// 增加一些要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <param name="rebuildFeature">是否需要根据属性和图形新建要素。若源要素属于另一个拥有不同字段的要素类，则该值应设为True</param>
        /// <returns></returns>
        public async Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source, bool rebuildFeature)
        {
            ThrowIfNotEditable();
            Dictionary<string, FieldInfo> key2Field = (this is IHasDefaultFields ? Fields.IncludeDefaultFields() : Fields)
                .ToDictionary(p => p.Name);

            //为了避免出现问题，改成了强制重建，经测试用不了多长时间
            if (true || rebuildFeature)
            {
                List<Feature> newFeatures = new List<Feature>();
                foreach (var feature in features)
                {
                    Feature newFeature = feature.Clone(this, key2Field);
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
            ThrowIfNotEditable();
            await table.DeleteFeatureAsync(feature);
            NotifyFeaturesChanged(null, new[] { feature }, null, source);
        }

        public async Task DeleteFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            await table.DeleteFeaturesAsync(features);
            NotifyFeaturesChanged(null, features, null, source);
        }

        public async Task UpdateFeatureAsync(UpdatedFeature feature, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            await table.UpdateFeatureAsync(feature.Feature);
            NotifyFeaturesChanged(null, null, new[] { feature }, source);
        }

        public async Task UpdateFeaturesAsync(IEnumerable<UpdatedFeature> features, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            await table.UpdateFeaturesAsync(features.Select(p => p.Feature));
            NotifyFeaturesChanged(null, null, features, source);
        }

        public Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry)
        {
            ThrowIfNotEditable();
            return table.CreateFeature(attributes, geometry);
        }

        public Feature CreateFeature()
        {
            ThrowIfNotEditable();
            return table.CreateFeature();
        }
    }
}