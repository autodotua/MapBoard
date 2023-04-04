using System.ComponentModel;

namespace MapBoard.UI.Model
{
    /// <summary>
    /// 可被选择的对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SelectableObject<T> : INotifyPropertyChanged
    {
        public bool IsSelected { get; set; }
        public T ObjectData { get; set; }

        public SelectableObject(T objectData)
        {
            ObjectData = objectData;
        }

        public SelectableObject(T objectData, bool isSelected)
        {
            IsSelected = isSelected;
            ObjectData = objectData;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}