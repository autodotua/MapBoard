using FzLib;
using System.ComponentModel;

namespace MapBoard.Mapping.Model
{
    public class GroupInfo : INotifyPropertyChanged
    {
        public GroupInfo(string name, bool? visiable, bool isNull = false)
        {
            Name = name;
            Visiable = visiable;
            IsNull = isNull;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsNull { get; set; }
        public string Name { get; set; }
        public bool? Visiable { get; set; }
    }
}