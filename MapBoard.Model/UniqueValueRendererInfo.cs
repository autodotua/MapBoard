using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Model
{
    public class UniqueValueRendererInfo : Dictionary<string, SymbolInfo>, INotifyPropertyChanged
    {
        public string KeyFieldName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SymbolInfo DefaultSymbol { get; set; } = new SymbolInfo();
        public bool HasCustomSymbols => !string.IsNullOrEmpty(KeyFieldName) && Count > 0;

        public new SymbolInfo this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }
                return base[key];
            }
            set => base[key] = value;
        }
    }
}