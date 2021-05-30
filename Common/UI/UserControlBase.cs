using System.ComponentModel;
using System.Windows.Controls;

namespace MapBoard.Common
{
    public abstract class UserControlBase : UserControl, INotifyPropertyChanged
    {
        public UserControlBase()
        {
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}