using Esri.ArcGISRuntime.Data;
using FzLib;
using MapBoard.Model;
using MapBoard.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public class FeatureAttributeCollection : INotifyPropertyChanged
    {
        private List<FeatureAttribute> all = new List<FeatureAttribute>();
        private bool isSelected;
        private List<FeatureAttribute> others = new List<FeatureAttribute>();

        private FeatureAttributeCollection()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IReadOnlyList<FeatureAttribute> All => all.AsReadOnly();

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

        public IReadOnlyList<FeatureAttribute> Others => others.AsReadOnly();

        public static FeatureAttributeCollection Empty(ILayerInfo layer)
        {
            var attributes = new FeatureAttributeCollection();

            foreach (var field in layer.Fields)
            {
                var attr = new FeatureAttribute(field);
                attributes.others.Add(attr);
                attributes.all.Add(attr);
            }
            return attributes;
        }

        public static FeatureAttributeCollection FromFeature(ILayerInfo layer, Feature feature)
        {
            FeatureAttributeCollection attributes = new FeatureAttributeCollection
            {
                Feature = feature
            };

            if (feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName))
            {
                var createTimeString = feature.Attributes[Parameters.CreateTimeFieldName] as string;
                DateTime? createTime = null;
                try
                {
                    createTime = string.IsNullOrEmpty(createTimeString) ? null : DateTime.Parse(createTimeString);
                }
                catch
                {
                }
                attributes.all.Add(new FeatureAttribute(FieldExtension.CreateTimeField, createTime));
            }
            if (feature.Attributes.ContainsKey(Parameters.ModifiedTimeFieldName))
            {
                var createTimeString = feature.Attributes[Parameters.ModifiedTimeFieldName] as string;
                DateTime? createTime = null;
                try
                {
                    createTime = string.IsNullOrEmpty(createTimeString) ? null : DateTime.Parse(createTimeString);
                }
                catch
                {
                }
                attributes.all.Add(new FeatureAttribute(FieldExtension.CreateTimeField, createTime));
            }

            foreach (var attr in feature.Attributes.Where(p => p.Key is not "FID" or "ObjectID" or Parameters.CreateTimeFieldName or Parameters.ModifiedTimeFieldName))
            {
                FeatureAttribute newAttr = null;
                if (layer.Fields.Any(p => p.Name == attr.Key))
                {
                    var field = layer.Fields.First(p => p.Name == attr.Key);
                    newAttr = new FeatureAttribute(field, attr.Value);
                }
                else
                {
                    newAttr = new FeatureAttribute()
                    {
                        Name = attr.Key,
                        DisplayName = attr.Key,
                        Type = feature.FeatureTable.Fields.First(p => p.Name == attr.Key).ToFieldInfo().Type,
                        Value = attr.Value
                    };
                }
                attributes.others.Add(newAttr);
                attributes.all.Add(newAttr);
            }
            return attributes;
        }

        public void SaveToFeature()
        {
            SaveToFeature(Feature);
        }

        public void SaveToFeature(Feature feature)
        {
            foreach (var attr in All)
            {
                switch (attr.Type)
                {
                    case FieldInfoType.Date:
                        feature.SetAttributeValue(attr.Name, attr.DateValue.HasValue ? new DateTimeOffset(attr.DateValue.Value, TimeSpan.Zero) : (DateTimeOffset?)null);

                        break;

                    case FieldInfoType.Time:
                        feature.SetAttributeValue(attr.Name, attr.TimeValue.HasValue ? attr.TimeValue.Value.ToString(Parameters.TimeFormat) : null);
                        break;

                    default:
                        feature.SetAttributeValue(attr.Name, attr.Value);
                        break;
                }
            }
        }
    }
}