using Esri.ArcGISRuntime.Data;
using MapBoard.Common.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Model
{
    public class FeatureAttributes : INotifyPropertyChanged
    {
        private FeatureAttributes()
        {
        }

        public DateTimeOffset? Date { get; set; }
        public string Label { get; set; }
        public string Key { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public static FeatureAttributes FromFeature(Feature feature)
        {
            FeatureAttributes attributes = new FeatureAttributes();
            attributes.Label = feature.Attributes[Resource.DisplayFieldName] as string;
            attributes.Key = feature.Attributes[Resource.KeyFieldName] as string;
            if (feature.Attributes[Resource.TimeExtentFieldName] is DateTimeOffset date)
            {
                attributes.Date = date;
            }
            else
            {
                attributes.Date = null;
            }
            return attributes;
        }

        public void SaveToFeature(Feature feature)
        {
            if (!string.IsNullOrWhiteSpace(Label))
            {
                feature.Attributes[Resource.DisplayFieldName] = Label;
            }
            if (Date.HasValue)
            {
                Date = new DateTimeOffset(Date.Value.DateTime, TimeSpan.Zero);
                feature.Attributes[Resource.TimeExtentFieldName] = Date.Value.UtcDateTime;
            }
            else
            {
                feature.Attributes[Resource.TimeExtentFieldName] = null;
            }
            feature.Attributes[Resource.KeyFieldName] = Key;
        }
    }
}