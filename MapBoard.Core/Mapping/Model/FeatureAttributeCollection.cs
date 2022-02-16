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

        public DateTime? Date
        {
            get => all.FirstOrDefault(p => p.Name == Parameters.DateFieldName)?.DateValue;
            set
            {
                var item = all.First(p => p.Name == Parameters.DateFieldName);
                item.DateValue = value;
            }
        }

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

        public string Key
        {
            get => all.FirstOrDefault(p => p.Name == Parameters.ClassFieldName)?.TextValue;
            set
            {
                var item = all.First(p => p.Name == Parameters.ClassFieldName);
                item.TextValue = value;
            }
        }

        public string Label
        {
            get => all.FirstOrDefault(p => p.Name == Parameters.LabelFieldName)?.TextValue;
            set
            {
                var item = all.First(p => p.Name == Parameters.LabelFieldName);
                item.TextValue = value;
            }
        }

        public IReadOnlyList<FeatureAttribute> Others => others.AsReadOnly();

        public static FeatureAttributeCollection Empty(ILayerInfo layer)
        {
            var attributes = new FeatureAttributeCollection();
            attributes.all.Add(new FeatureAttribute(FieldExtension.LabelField, null));
            attributes.all.Add(new FeatureAttribute(FieldExtension.ClassField, null));
            attributes.all.Add(new FeatureAttribute(FieldExtension.DateField, null));

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

            if (feature.Attributes.ContainsKey(Parameters.LabelFieldName))
            {
                attributes.all.Add(new FeatureAttribute(FieldExtension.LabelField, feature.Attributes[Parameters.LabelFieldName] as string));
            }
            if (feature.Attributes.ContainsKey(Parameters.ClassFieldName))
            {
                attributes.all.Add(new FeatureAttribute(FieldExtension.ClassField, feature.Attributes[Parameters.ClassFieldName] as string));
            }
            if (feature.Attributes.ContainsKey(Parameters.DateFieldName))
            {
                attributes.all.Add(new FeatureAttribute(FieldExtension.DateField, (feature.Attributes[Parameters.DateFieldName] as DateTimeOffset?)?.UtcDateTime));
            }
            if (feature.Attributes.ContainsKey(Parameters.CreateTimeFieldName))
            {
                var createTimeString = feature.Attributes[Parameters.CreateTimeFieldName] as string;
                DateTime? createTime = null;
                try
                {
                    createTime = string.IsNullOrEmpty(createTimeString) ? (DateTime?)null : DateTime.Parse(createTimeString);
                }
                catch
                {
                }
                attributes.all.Add(new FeatureAttribute(FieldExtension.CreateTimeField, createTime));
            }

            foreach (var attr in feature.Attributes.GetCustomAttributes())
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