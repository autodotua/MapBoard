using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ModernWpf.FzExtension
{
    /// <summary>
    /// ProgressRingOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressRingOverlay : UserControl, INotifyPropertyChanged
    {
        public ProgressRingOverlay()
        {
            TaskArgs = new ProgressRingOverlayArgs(this);
            InitializeComponent();
        }

        public void Show()
        {
            DoubleAnimation ani = new DoubleAnimation(1, TimeSpan.FromMilliseconds(500));
            ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };
            grd.BeginAnimation(OpacityProperty, ani);
            grd.Visibility = Visibility.Visible;
            grd.IsHitTestVisible = true;
        }

        public void Hide()
        {
            DoubleAnimation ani = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
            ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };
            grd.IsHitTestVisible = false;
            ani.Completed += (p1, p2) =>
            {
                grd.Visibility = Visibility.Collapsed;
            };
            grd.BeginAnimation(OpacityProperty, ani);
        }

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
              nameof(Message),
              typeof(string),
              typeof(ProgressRingOverlay));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private TaskCompletionSource taskSource = null;

        public ProgressRingOverlayArgs TaskArgs { get; }
    }

    public class ProgressRingOverlayArgs
    {
        public ProgressRingOverlayArgs(ProgressRingOverlay ui)
        {
            this.ui = ui;
        }

        private ProgressRingOverlay ui;

        public void SetMessage(string message)
        {
            ui.Message = message;
        }
    }
}