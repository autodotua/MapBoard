using Esri.ArcGISRuntime.Data;
using FzLib;
using MapBoard.Model;
using MapBoard.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 要素属性集合。单个要素的所有属性的集合。
    /// </summary>
    public class FeatureAttributeCollection : INotifyPropertyChanged
    {
        private List<FeatureAttribute> all = new List<FeatureAttribute>();
        private bool isSelected;

        private FeatureAttributeCollection()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IReadOnlyList<FeatureAttribute> Attributes => all.AsReadOnly();

        [JsonIgnore]
        public Feature Feature { get; set; }

        [JsonIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value)
                {
                    return;
                }
                isSelected = value;
            }
        }

        /// <summary>
        /// 创建一个属性值为空的<see cref="FeatureAttributeCollection"/>
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static FeatureAttributeCollection Empty(ILayerInfo layer)
        {
            var attributes = new FeatureAttributeCollection();

            foreach (var field in layer.Fields)
            {
                var attr = FeatureAttribute.FromFieldAndValue(field);
                attributes.all.Add(attr);
            }
            return attributes;
        }

        /// <summary>
        /// 从要素读取属性并返回<see cref="FeatureAttributeCollection"/>
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        public static FeatureAttributeCollection FromFeature(ILayerInfo layer, Feature feature)
        {
            FeatureAttributeCollection attributes = new FeatureAttributeCollection
            {
                Feature = feature
            };
            IEnumerable<KeyValuePair<string, object>> featureAttributes = feature.Attributes
                .Where(p => !FieldExtension.IsIdField(p.Key));
            //重新排序，使属性（Attributes）的顺序与创建图层时设置的字段（Fields）顺序相同
            try
            {
                var layerFieldNames = layer.Fields.Select(p => p.Name).ToList();
                 featureAttributes = feature.Attributes
                    .Where(p => !FieldExtension.IsIdField(p.Key))
                    .OrderBy(p => layerFieldNames.IndexOf(p.Key))
                    .ToList();
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Attributes排序失败");
                Debug.WriteLine(ex);
            }

            //填充属性
            foreach (var attr in featureAttributes)
            {
                FeatureAttribute newAttr = null;
               
                if (layer.Fields.Any(p => p.Name == attr.Key)) //根据预定义的字段生成属性
                {
                    var field = layer.Fields.First(p => p.Name == attr.Key);
                    newAttr = FeatureAttribute.FromFieldAndValue(field, attr.Value);
                }
                else //根据实际属性类型定义属性
                {
                    newAttr = new FeatureAttribute()
                    {
                        Name = attr.Key,
                        DisplayName = attr.Key,
                        Type = feature.FeatureTable.Fields.First(p => p.Name == attr.Key).ToFieldInfo().Type,
                        Value = attr.Value
                    };
                }
                attributes.all.Add(newAttr);
            }
            return attributes;
        }

        /// <summary>
        /// 将当前的属性集合写入到当前要素
        /// </summary>
        public void SaveToFeature()
        {
            SaveToFeature(Feature);
        }

        /// <summary>
        /// 将当前的属性集合写入到指定要素
        /// </summary>
        /// <param name="feature"></param>
        public void SaveToFeature(Feature feature)
        {
            foreach (var attr in Attributes)
            {
                feature.SetAttributeValue(attr.Name, attr.Value);
            }
        }
    }
}