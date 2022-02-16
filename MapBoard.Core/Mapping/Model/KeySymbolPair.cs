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

        public string Key { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SymbolInfo Symbol { get; set; }
    }
}