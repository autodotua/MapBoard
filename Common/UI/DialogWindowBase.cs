using ModernWpf.Controls.Primitives;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MapBoard.Common
{
    public class DialogWindowBase : WindowBase, INotifyPropertyChanged
    {
        public DialogWindowBase(Window owner)
        {
            Owner = owner;
            WindowHelper.SetUseModernWindowStyle(this, true);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;
            ShowInTaskbar = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}