using System;
using System.ComponentModel;
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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}