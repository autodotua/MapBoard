using System.ComponentModel;

namespace MapBoard.UI.Model
{
    /// <summary>
    /// 属性的名称-值类型
    /// </summary>
    public class PropertyNameValue : INotifyPropertyChanged
    {
        public PropertyNameValue()
        {
        }

        public PropertyNameValue(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }

        public string Value { get; set; }
    }
}