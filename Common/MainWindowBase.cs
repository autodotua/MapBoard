using FzLib.Control.Dialog;
using FzLib.Control.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
