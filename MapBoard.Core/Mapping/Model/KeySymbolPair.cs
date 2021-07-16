using FzLib;
using MapBoard.Model;
using System.ComponentModel;

namespace MapBoard.Mapping.Model
{
    public class KeySymbolPair : INotifyPropertyChanged
    {
        public KeySymbolPair()
        {
        }

        public KeySymbolPair(string key, SymbolInfo symbol)
        {
            Key = key;
            Symbol = symbol;
        }

        private string key;

        public string Key
        {
            get => key;
            set => this.SetValueAndNotify(ref key, value, nameof(Key));
        }

        private SymbolInfo symbol;

        public event PropertyChangedEventHandler PropertyChanged;

        public SymbolInfo Symbol
        {
            get => symbol;
            set => this.SetValueAndNotify(ref symbol, value, nameof(Symbol));
        }
    }
}