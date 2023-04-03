using System.ComponentModel;
using System.Windows.Controls;

namespace MapBoard.UI
{
    /// <summary>
    /// 用户控件基类。设置数据上下文为自身。
    /// </summary>
    public abstract class UserControlBase : UserControl, INotifyPropertyChanged
    {
        public UserControlBase()
        {
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}