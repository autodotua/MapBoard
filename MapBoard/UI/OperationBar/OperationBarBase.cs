using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MapBoard.UI.OperationBar
{
    public abstract class OperationBarBase : Grid, INotifyPropertyChanged
    {
        public OperationBarBase() : base()
        {
            DataContext = this;
            Height = 24;
            Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x30));

            ani = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
            ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };
            Storyboard.SetTarget(ani, this);
            Storyboard.SetTargetProperty(ani, new PropertyPath("(Grid.RenderTransform).(TranslateTransform.Y)"));
            storyboard = new Storyboard() { Children = { ani } };
            //storyboard.Begin();
        }
        DoubleAnimation ani;
        Storyboard storyboard;
        public void Show()
        {
            ani.To = 0;
            storyboard.Begin();
        }

        public void Hide()
        {
            ani.To = -24;
            storyboard.Begin();
        }

        protected void Notify(params string[] names)
        {
            foreach (var name in names)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
