using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapBoard.UI.Dialog
{
    public abstract class RightBottomFloatDialogBase : DialogWindowBase
    {
        protected RightBottomFloatDialogBase(Window owner) : base(owner)
        {
            Loaded += (s, e) => ResetLocation();
            owner.IsVisibleChanged += (p1, p2) =>
            {
                if (p2.NewValue.Equals(false))
                {
                    Visibility = Visibility.Collapsed;
                }
            };
        }

        protected virtual int OffsetX { get; } = 0;
        protected virtual int OffsetY { get; } = 0;
        public void ResetLocation()
        {
            if (IsClosed)
            {
                return;
            }
            double left = Owner.Left - OffsetX;
            double top = Owner.Top - OffsetY;
            if (Owner.WindowState == WindowState.Maximized)
            {
                left = -OffsetX-12;
                top = -OffsetY;
            }


            Left = left + Owner.ActualWidth - ActualWidth;
            Top = top + Owner.ActualHeight - Height;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            ResetLocation();
            base.OnContentRendered(e);
        }
    }
}
