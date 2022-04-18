using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Model
{
    public class UniqueValueRendererInfo : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public int Count => Symbols.Count;

        public SymbolInfo DefaultSymbol { get; set; }

        [JsonIgnore]
        public bool HasCustomSymbols => !string.IsNullOrEmpty(KeyFieldName) && Symbols.Count > 0;

        public string KeyFieldName { get; set; }
        public Dictionary<string, SymbolInfo> Symbols { get; set; } = new Dictionary<string, SymbolInfo>();

        public object Clone()
        {
            var info = MemberwiseClone() as UniqueValueRendererInfo;
            info.Symbols = new Dictionary<string, SymbolInfo>(Symbols);
            foreach (var key in info.Symbols.Keys)
            {
                info.Symbols[key] = info.Symbols[key].Clone() as SymbolInfo;
            }
            return info;
        }
    }
}