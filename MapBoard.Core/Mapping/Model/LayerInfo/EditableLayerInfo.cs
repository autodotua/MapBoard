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
    /// <summary>
    /// 可编辑图层基类
    /// </summary>
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
            if(Histories.Count>0)
            {
                Histories.Clear();
            }
        }

        /// <summary>
        /// 要素集合发生改变，即增删改
        /// </summary>
        public event EventHandler<FeaturesChangedEventArgs> FeaturesChanged;

        /// <summary>
        /// 要素操作历史
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<FeaturesChangedEventArgs> Histories { get; private set; } = new ObservableCollection<FeaturesChangedEventArgs>();

        /// <summary>
        /// 新增要素
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public Task AddFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            return AddFeatureAsync(feature, source, feature.FeatureTable != table);
        }

        /// <summary>
        /// 新增要素
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <param name="rebuildFeature">根据原有要素的图形和属性，创建新的要素，而不是使用旧的要素</param>
        /// <returns></returns>
        public async Task AddFeatureAsync(Feature feature, FeaturesChangedSource source, bool rebuildFeature)
        {
            ThrowIfNotEditable();
            Feature newFeature = rebuildFeature ? feature.Clone(this) : feature;
            AddCreateTimeAttributeIfExistField(newFeature);
            await table.AddFeatureAsync(newFeature);
            NotifyFeaturesChanged(new[] { feature }, null, null, source);
        }

        /// <summary>
        /// 新增一些要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Task<IEnumerable<Feature>> AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
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
        public async Task<IEnumerable<Feature>> AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source, bool rebuildFeature)
        {
            ThrowIfNotEditable();
            Dictionary<string, FieldInfo> key2Field = Fields.ToDictionary(p => p.Name);
            //为了避免出现问题，改成了强制重建，经测试用不了多长时间
            if (true || rebuildFeature)
            {
                List<Feature> newFeatures = new List<Feature>();
                foreach (var feature in features)
                {
                    Feature newFeature = feature.Clone(table, key2Field);
                    newFeatures.Add(newFeature);
                }
                await table.AddFeaturesAsync(newFeatures);
                NotifyFeaturesChanged(newFeatures, null, null, source);
                features = newFeatures;
            }
            else
            {
                foreach (var feature in features)
                {
                    if (!feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName)
                        || feature.Attributes[Parameters.CreateTimeFieldName] == null)
                    {
                        AddCreateTimeAttributeIfExistField(feature);
                    }
                    else
                    {
                        AddModifiedTimeAttributeIfExistField(feature);
                    }
                }
                await table.AddFeaturesAsync(features);
                NotifyFeaturesChanged(features, null, null, source);
            }
            return features;
        }

        /// <summary>
        /// 根据属性和图形创建要素
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry)
        {
            ThrowIfNotEditable();
            return table.CreateFeature(attributes, geometry);
        }

        /// <summary>
        /// 创建空要素
        /// </summary>
        /// <returns></returns>
        public Feature CreateFeature()
        {
            ThrowIfNotEditable();
            return table.CreateFeature();
        }

        /// <summary>
        /// 删除要素
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task DeleteFeatureAsync(Feature feature, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            await table.DeleteFeatureAsync(feature);
            NotifyFeaturesChanged(null, new[] { feature }, null, source);
        }

        /// <summary>
        /// 删除一些要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task DeleteFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            await table.DeleteFeaturesAsync(features);
            NotifyFeaturesChanged(null, features, null, source);
        }

        /// <summary>
        /// 更新要素
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task UpdateFeatureAsync(UpdatedFeature feature, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            AddModifiedTimeAttributeIfExistField(feature.Feature);
            await table.UpdateFeatureAsync(feature.Feature);
            NotifyFeaturesChanged(null, null, new[] { feature }, source);
        }

        /// <summary>
        /// 更新一些要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task UpdateFeaturesAsync(IEnumerable<UpdatedFeature> features, FeaturesChangedSource source)
        {
            ThrowIfNotEditable();
            features.ForEach(feature => AddModifiedTimeAttributeIfExistField(feature.Feature));
            await table.UpdateFeaturesAsync(features.Select(p => p.Feature));
            NotifyFeaturesChanged(null, null, features, source);
        }

        /// <summary>
        /// 增加创建时间字段，如果该字段存在
        /// </summary>
        /// <param name="feature"></param>
        private void AddCreateTimeAttributeIfExistField(Feature feature)
        {
            if (table.Fields.Any(p => p.Name == Parameters.CreateTimeFieldName)
                && table.Fields.First(p => p.Name == Parameters.CreateTimeFieldName).FieldType==FieldType.Text)
            {
                if (!feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName)
                        || feature.Attributes[Parameters.CreateTimeFieldName] == null)
                {
                    feature.SetAttributeValue(Parameters.CreateTimeFieldName, DateTime.Now.ToString(Parameters.TimeFormat));
                }
            }
        }

        /// <summary>
        /// 增加修改时间字段，如果该字段存在
        /// </summary>
        /// <param name="feature"></param>
        private void AddModifiedTimeAttributeIfExistField(Feature feature)
        {
            if (table.Fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName)
                && table.Fields.First(p => p.Name == Parameters.ModifiedTimeFieldName).FieldType == FieldType.Text)
            {
                feature.SetAttributeValue(Parameters.ModifiedTimeFieldName, DateTime.Now.ToString(Parameters.TimeFormat));
            }
        }

        /// <summary>
        /// 通知要素发生改变
        /// </summary>
        /// <param name="added"></param>
        /// <param name="deleted"></param>
        /// <param name="updated"></param>
        /// <param name="source"></param>
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

        /// <summary>
        /// 检测是否允许编辑，若不允许则抛出错误
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        private void ThrowIfNotEditable()
        {
            if (!Interaction.CanEdit)
            {
                throw new NotSupportedException("当前图层被禁止编辑");
            }
        }
    }
}