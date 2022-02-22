using FzLib;
using System.ComponentModel;

namespace MapBoard.Mapping.Model
{
    public class GroupInfo : INotifyPropertyChanged
    {
        public GroupInfo(string name, bool? visible, bool isNull = false)
        {
            Name = name;
            Visible = visible;
            IsNull = isNull;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsNull { get; set; }
        public string Name { get; set; }
        public bool? Visible { get; set; }
    }
}