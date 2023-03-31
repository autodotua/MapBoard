using FzLib;
using MapBoard.Model;
using System.ComponentModel;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 唯一值渲染器中，唯一值的值和对应的符号
    /// </summary>
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

        public event PropertyChangedEventHandler PropertyChanged;

        public string Key { get; set; }

        public SymbolInfo Symbol { get; set; }
    }
}