using Esri.ArcGISRuntime.Data;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Main.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Model
{
    public class FeatureAttributes : INotifyPropertyChanged
    {
        private List<FeatureAttribute> others = new List<FeatureAttribute>();
        private List<FeatureAttribute> all = new List<FeatureAttribute>();

        private FeatureAttributes()
        {
        }

        [JsonIgnore]
        private Feature feature;

        [JsonIgnore]
        public Feature Feature
        {
            get => feature;
            private set => this.SetValueAndNotify(ref feature, value, nameof(Feature));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Label
        {
            get => all.FirstOrDefault(p => p.Name == Resource.LabelFieldName)?.TextValue;
            set
            {
                var item = all.First(p => p.Name == Resource.LabelFieldName);
                item.TextValue = value;
                this.Notify(nameof(Label));
            }
        }

        public string Key
        {
            get => all.FirstOrDefault(p => p.Name == Resource.ClassFieldName)?.TextValue;
            set
            {
                var item = all.First(p => p.Name == Resource.ClassFieldName);
                item.TextValue = value;
                this.Notify(nameof(Key));
            }
        }

        public DateTimeOffset? Date
        {
            get => all.FirstOrDefault(p => p.Name == Resource.DateFieldName)?.DateValue;
            set
            {
                var item = all.First(p => p.Name == Resource.DateFieldName);
                item.DateValue = value;
                this.Notify(nameof(Date));
            }
        }

        public IReadOnlyList<FeatureAttribute> Others => others.AsReadOnly();
        public IReadOnlyList<FeatureAttribute> All => all.AsReadOnly();

        public static FeatureAttributes Empty(LayerInfo layer)
        {
            var attrs = new FeatureAttributes();
            foreach (var field in layer.Fields)
            {
                attrs.others.Add(new FeatureAttribute()
                {
                    Name = field.Name,
                    DisplayName = field.DisplayName,
                    Type = field.Type
                });
            }
            return attrs;
        }

        public static FeatureAttributes FromFeature(LayerInfo layer, Feature feature)
        {
            FeatureAttributes attributes = new FeatureAttributes
            {
                Feature = feature
            };

            attributes.all.Add(new FeatureAttribute(FieldInfo.LabelField, feature.Attributes[Resource.LabelFieldName] as string));
            attributes.all.Add(new FeatureAttribute(FieldInfo.ClassField, feature.Attributes[Resource.ClassFieldName] as string));
            attributes.all.Add(new FeatureAttribute(FieldInfo.DateField, feature.Attributes[Resource.DateFieldName] as DateTimeOffset?));
            foreach (var attr in FieldUtility.GetCustomAttributes(feature.Attributes))
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
                feature.Attributes[attr.Name] = attr.Value;
            }
        }
    }
}