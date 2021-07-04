using FzLib.Extension;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using static FzLib.Extension.ExtendedINotifyPropertyChanged;

namespace MapBoard.Model
{
    [DebuggerDisplay("{Name}")]
    public class LayerInfo : INotifyPropertyChanged, ICloneable
    {
        public string Name { get; set; }

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

        public LabelInfo Label { get; set; } = new LabelInfo();

        public virtual object Clone()
        {
            LayerInfo layer = MemberwiseClone() as LayerInfo;
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