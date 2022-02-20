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
        private FieldInfo[] fields;

        public event PropertyChangedEventHandler PropertyChanged;

        public string DefinitionExpression { get; set; } = "";
        public LayerDisplay Display { get; set; } = new LayerDisplay();

        public virtual FieldInfo[] Fields
        {
            get
            {
                if (fields == null)
                {
                    fields = Array.Empty<FieldInfo>();
                }
                return fields;
            }
            set => fields = value;
        }

        public string Group { get; set; }
        public LayerInteraction Interaction { get; set; } = new LayerInteraction();
        public LabelInfo[] Labels { get; set; }

        public virtual bool LayerVisible { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> ServiceParameters { get; } = new Dictionary<string, string>();
        public UniqueValueRendererInfo Renderer { get; set; } = new UniqueValueRendererInfo();

        [JsonProperty]
        public virtual string Type { get; protected set; }

        public virtual object Clone()
        {
            LayerInfo layer = MemberwiseClone() as LayerInfo;
            foreach (var key in Renderer.Symbols.Keys.ToList())
            {
                layer.Renderer.Symbols[key] = Renderer.Symbols[key].Clone() as SymbolInfo;
            }
            layer.fields = fields?.Select(p => p.Clone() as FieldInfo).ToArray();
            layer.Labels = Labels?.Select(p => p.Clone() as LabelInfo).ToArray();
            return layer;
        }
    }
}