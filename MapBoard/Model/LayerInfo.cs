using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Extension;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using static FzLib.Extension.ExtendedINotifyPropertyChanged;
using static MapBoard.Main.Util.LayerUtility;

namespace MapBoard.Main.Model
{
    [DebuggerDisplay("{Name}")]
    public class LayerInfo : INotifyPropertyChanged, ICloneable
    {
        public string Name { get; set; }

        public Dictionary<string, SymbolInfo> Symbols { get; set; } = new Dictionary<string, SymbolInfo>();

        private ShapefileFeatureTable table;

        [JsonIgnore]
        public ShapefileFeatureTable Table
        {
            get => table;
            set => this.SetValueAndNotify(ref table, value, nameof(LayerVisible));
        }

        [JsonIgnore]
        public FeatureLayer Layer => Table?.Layer as FeatureLayer;

        private bool layerVisible = true;

        public bool LayerVisible
        {
            get => layerVisible;
            set
            {
                layerVisible = value;
                if (Layer != null)
                {
                    Layer.IsVisible = value;
                }
                this.Notify(nameof(LayerVisible));
            }
        }

        private FieldInfo[] fields;

        public FieldInfo[] Fields
        {
            get
            {
                if (fields == null)
                {
                    fields = Array.Empty<FieldInfo>();
                }
                return fields;
            }
            set => this.SetValueAndNotify(ref fields, value, nameof(Fields));
        }

        private TimeExtentInfo timeExtent;

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeExtentInfo TimeExtent
        {
            get => timeExtent;
            set => this.SetValueAndNotify(ref timeExtent, value, nameof(TimeExtentEnable));
        }

        [JsonIgnore]
        public bool TimeExtentEnable
        {
            get => TimeExtent == null ? false : TimeExtent.IsEnable;
            set
            {
                if (TimeExtent != null)
                {
                    if (value != TimeExtent.IsEnable)
                    {
                        TimeExtent.IsEnable = value;
                        this.SetTimeExtentAsync();
                    }
                }

                this.Notify(nameof(TimeExtentEnable));
            }
        }

        public LabelInfo Label { get; set; } = new LabelInfo();

        public void NotifyFeatureChanged()
        {
            this.Notify(nameof(Table));
        }

        public object Clone()
        {
            LayerInfo layer = MemberwiseClone() as LayerInfo;
            layer.Table = null;
            foreach (var key in Symbols.Keys.ToList())
            {
                layer.Symbols[key] = Symbols[key].Clone() as SymbolInfo;
            }
            layer.fields = fields == null ? null : fields.Select(p => p.Clone() as FieldInfo).ToArray();
            layer.Label = Label.Clone() as LabelInfo;
            return layer;
        }
    }
}