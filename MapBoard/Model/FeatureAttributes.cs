using Esri.ArcGISRuntime.Data;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Main.Util;
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
        private DateTimeOffset? date;

        private string key;

        private string label;

        private List<FeatureAttribute> others = new List<FeatureAttribute>();

        private FeatureAttributes()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTimeOffset? Date
        {
            get => date;
            set => this.SetValueAndNotify(ref date, value, nameof(Date));
        }

        public string Key
        {
            get => key;
            set => this.SetValueAndNotify(ref key, value, nameof(Key));
        }

        public string Label
        {
            get => label;
            set => this.SetValueAndNotify(ref label, value, nameof(Label));
        }

        public IReadOnlyList<FeatureAttribute> Others => others.AsReadOnly();

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
            FeatureAttributes attributes = new FeatureAttributes();
            attributes.Label = feature.Attributes[Resource.LabelFieldName] as string;
            attributes.Key = feature.Attributes[Resource.ClassFieldName] as string;
            if (feature.Attributes[Resource.DateFieldName] is DateTimeOffset date)
            {
                attributes.Date = date;
            }
            else
            {
                attributes.Date = null;
            }
            foreach (var attr in FieldUtility.GetCustomAttributes(feature.Attributes))
            {
                if (layer.Fields.Any(p => p.Name == attr.Key))
                {
                    var field = layer.Fields.First(p => p.Name == attr.Key);
                    attributes.others.Add(new FeatureAttribute()
                    {
                        Name = attr.Key,
                        DisplayName = field.DisplayName,
                        Type = field.Type,
                        Value = attr.Value
                    });
                }
                else
                {
                    attributes.others.Add(new FeatureAttribute()
                    {
                        Name = attr.Key,
                        DisplayName = attr.Key,
                        Type = feature.FeatureTable.Fields.First(p => p.Name == attr.Key).ToFieldInfo().Type,
                        Value = attr.Value
                    });
                }
            }
            return attributes;
        }

        public void SaveToFeature(Feature feature)
        {
            if (!string.IsNullOrWhiteSpace(Label))
            {
                feature.Attributes[Resource.LabelFieldName] = Label;
            }
            if (Date.HasValue)
            {
                Date = new DateTimeOffset(Date.Value.DateTime, TimeSpan.Zero);
                feature.Attributes[Resource.DateFieldName] = Date.Value.UtcDateTime;
            }
            else
            {
                feature.Attributes[Resource.DateFieldName] = null;
            }
            feature.Attributes[Resource.ClassFieldName] = Key;
            if (Others != null)
            {
                foreach (var attr in Others)
                {
                    feature.Attributes[attr.Name] = attr.Value;
                }
            }
        }
    }
}