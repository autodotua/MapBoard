using Esri.ArcGISRuntime.Data;
using FzLib.Extension;
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

        private DateTimeOffset? date;

        public DateTimeOffset? Date
        {
            get => date;
            set => this.SetValueAndNotify(ref date, value, nameof(Date));
        }

        private string label;

        public string Label
        {
            get => label;
            set => this.SetValueAndNotify(ref label, value, nameof(Label));
        }

        private string key;

        public string Key
        {
            get => key;
            set => this.SetValueAndNotify(ref key, value, nameof(Key));
        }

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

        public static FeatureAttributes Empty => new();

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