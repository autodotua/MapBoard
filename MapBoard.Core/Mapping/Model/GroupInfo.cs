using FzLib.Extension;
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

        private string name;

        public string Name
        {
            get => name;
            set => this.SetValueAndNotify(ref name, value, nameof(Name));
        }

        private bool isNull;

        public bool IsNull
        {
            get => isNull;
            set => this.SetValueAndNotify(ref isNull, value, nameof(IsNull));
        }

        private bool? visiable;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool? Visiable
        {
            get => visiable;
            set => this.SetValueAndNotify(ref visiable, value, nameof(Visiable));
        }
    }
}