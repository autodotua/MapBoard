using FzLib.UI.Extension;
using System;

namespace MapBoard.Common
{
    public abstract class MainWindowBase : ExtendedWindow
    {
        public MainWindowBase()
        {
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }
    }
}