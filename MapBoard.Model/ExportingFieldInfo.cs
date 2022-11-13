using System.ComponentModel;
using System.Diagnostics;

namespace MapBoard.Model
{
    [DebuggerDisplay("Name={Name} Disp={DisplayName} Type={Type}")]
    public class ExportingFieldInfo : FieldInfo
    {
        public ExportingFieldInfo(string name, string displayName, FieldInfoType type)
        {
            Name = name;
            DisplayName = displayName;
            Type = type;
        }

        public ExportingFieldInfo()
        {
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Enable { get; set; }
        public FieldInfo OldField { get; set; }
        public bool CanEditType => OldField == null;
    }
}