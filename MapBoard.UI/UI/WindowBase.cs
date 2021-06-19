using System;
using System.ComponentModel;
using System.Windows;

namespace MapBoard.UI
{
    public abstract class WindowBase : Window, INotifyPropertyChanged
    {
        public WindowBase()
        {
            DataContext = this;
            WindowCreated?.Invoke(this, new EventArgs());
        }

        public static event EventHandler WindowCreated;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsClosed { get; private set; }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }

        public void BringToFront()
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            Topmost = true;  // important
            Topmost = false; // important
            Focus();
        }
    }
}