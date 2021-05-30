using ModernWpf.Controls.Primitives;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MapBoard.Common
{
    public class DialogWindowBase : WindowBase, INotifyPropertyChanged
    {
        public DialogWindowBase()
        {
            WindowHelper.SetUseModernWindowStyle(this, true);
            Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
            if (Owner == null)
            {
                Owner = Application.Current.MainWindow;
            }
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;
            ShowInTaskbar = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}