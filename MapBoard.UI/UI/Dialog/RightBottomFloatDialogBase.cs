using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 以小窗形式吸附在主窗口右下角的子窗口基类
    /// </summary>
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

        /// <summary>
        /// 水平偏移量
        /// </summary>
        protected virtual int OffsetX { get; } = 0;

        /// <summary>
        /// 垂直偏移量
        /// </summary>
        protected virtual int OffsetY { get; } = 0;

        /// <summary>
        /// 重置位置
        /// </summary>
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

        /// <summary>
        /// 窗口渲染完成，设置位置
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContentRendered(EventArgs e)
        {
            ResetLocation();
            base.OnContentRendered(e);
        }
    }
}
