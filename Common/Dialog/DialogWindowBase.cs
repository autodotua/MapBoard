using FzLib.UI.Extension;
using ModernWpf.Controls.Primitives;
using System.Linq;
using System.Windows;

namespace MapBoard.Common.Dialog
{
    public class DialogWindowBase : ExtendedWindow
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
    }
}