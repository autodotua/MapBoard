using FzLib;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace MapBoard.Model
{
    [DebuggerDisplay("{Name}")]
    public class LayerInfo : ICloneable, ILayerInfo
    {
        private string name;

        public string Name
        {
            get => name;
            set => this.SetValueAndNotify(ref name, value, nameof(Name));
        }

        public Dictionary<string, SymbolInfo> Symbols { get; set; } = new Dictionary<string, SymbolInfo>();

        private bool layerVisible = true;

        public virtual bool LayerVisible
        {
            get => layerVisible;
            set
            {
                layerVisible = value;
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

        private string group;

        public string Group
        {
            get => group;
            set => this.SetValueAndNotify(ref group, value, nameof(Group));
        }

        private TimeExtentInfo timeExtent;

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeExtentInfo TimeExtent
        {
            get => timeExtent;
            set => this.SetValueAndNotify(ref timeExtent, value, nameof(TimeExtent));
        }

        public LabelInfo[] Labels { get; set; }

        [JsonProperty]
        public virtual string Type { get; protected set; }

        [JsonIgnore]
        public virtual bool IsEditable { get; }

        public Dictionary<string, string> ServiceParameters { get; } = new Dictionary<string, string>();

        public virtual object Clone()
        {
            LayerInfo layer = MemberwiseClone() as LayerInfo;
            foreach (var key in Symbols.Keys.ToList())
            {
                layer.Symbols[key] = Symbols[key].Clone() as SymbolInfo;
            }
            layer.fields = fields?.Select(p => p.Clone() as FieldInfo).ToArray();
            layer.Labels = Labels?.Select(p => p.Clone() as LabelInfo).ToArray();
            return layer;
        }
    }
}