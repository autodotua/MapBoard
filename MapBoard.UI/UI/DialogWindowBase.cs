using ModernWpf.Controls.Primitives;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MapBoard.UI
{
    public class DialogWindowBase : WindowBase
    {
        public DialogWindowBase(Window owner)
        {
            Owner = owner;
            WindowHelper.SetUseModernWindowStyle(this, true);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;
            ShowInTaskbar = false;
            KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
        }
    }
}