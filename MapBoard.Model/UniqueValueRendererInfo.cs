using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Model
{
    public class UniqueValueRendererInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public int Count => Symbols.Count;

        public SymbolInfo DefaultSymbol { get; set; } = new SymbolInfo();

        [JsonIgnore]
        public bool HasCustomSymbols => !string.IsNullOrEmpty(KeyFieldName) && Symbols.Count > 0;

        public string KeyFieldName { get; set; }
        public Dictionary<string, SymbolInfo> Symbols { get; set; } = new Dictionary<string, SymbolInfo>();
    }
}