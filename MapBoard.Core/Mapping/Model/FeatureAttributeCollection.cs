﻿using Esri.ArcGISRuntime.Data;
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

        public static FeatureAttributeCollection FromFeature(ILayerInfo layer, Feature feature)
        {
            FeatureAttributeCollection attributes = new FeatureAttributeCollection
            {
                Feature = feature
            };

            foreach (var attr in feature.Attributes.Where(p => !FieldExtension.IsIdField(p.Key)))
            {
                FeatureAttribute newAttr = null;
                if (layer.Fields.Any(p => p.Name == attr.Key))
                {
                    var field = layer.Fields.First(p => p.Name == attr.Key);
                    newAttr = FeatureAttribute.FromFieldAndValue(field, attr.Value);
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
            foreach (var attr in Attributes)
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