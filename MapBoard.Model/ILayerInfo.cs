using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Model
{
    public interface ILayerInfo : INotifyPropertyChanged
    {
        FieldInfo[] Fields { get; set; }
        string Group { get; set; }
        bool IsWriteable { get; }
        LabelInfo Label { get; set; }
        bool LayerVisible { get; set; }
        string Name { get; set; }
        Dictionary<string, SymbolInfo> Symbols { get; set; }
        TimeExtentInfo TimeExtent { get; set; }
        string Type { get; }

        object Clone();
    }
}