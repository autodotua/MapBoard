using FzLib;
using System.ComponentModel;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 图层分组信息
    /// </summary>
    public class GroupInfo : INotifyPropertyChanged
    {
        public GroupInfo(string name, bool? visible, bool isNull = false)
        {
            Name = name;
            Visible = visible;
            IsNull = isNull;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 是否为默认组（未分组的图层所在的组）
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// 组名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否可见。null表示部分可见
        /// </summary>
        public bool? Visible { get; set; }
    }
}