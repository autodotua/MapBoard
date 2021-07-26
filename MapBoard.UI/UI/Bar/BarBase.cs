using MapBoard.Mapping;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MapBoard.Mapping.Model;
using FzLib.WPF;

namespace MapBoard.UI.Bar
{
    public abstract class BarBase : Grid, INotifyPropertyChanged
    {
        public static double DefaultBarHeight { get; } = 56;
        public virtual double ExpandDistance { get; } = DefaultBarHeight;
        public MainMapView MapView { get; set; }
        public MapLayerCollection Layers => MapView?.Layers;

        protected abstract ExpandDirection ExpandDirection { get; }
        private DoubleAnimation animation;
        private Storyboard storyboard = new Storyboard();

        public BarBase() : base()
        {
            DataContext = this;
            switch (ExpandDirection)
            {
                case ExpandDirection.Down:
                    RenderTransform = new TranslateTransform(0, -ExpandDistance);
                    break;

                case ExpandDirection.Up:
                    RenderTransform = new TranslateTransform(0, ExpandDistance);

                    break;

                case ExpandDirection.Left:
                    RenderTransform = new TranslateTransform(ExpandDistance, 0);
                    break;

                case ExpandDirection.Right:
                    RenderTransform = new TranslateTransform(-ExpandDistance, 0);
                    break;
            }
            SetResourceReference(BackgroundProperty, "SystemControlBackgroundAltHighBrush");
            string path = ExpandDirection switch
            {
                ExpandDirection.Down or ExpandDirection.Up => "(Grid.RenderTransform).(TranslateTransform.Y)",
                ExpandDirection.Left or ExpandDirection.Right => "(Grid.RenderTransform).(TranslateTransform.X)",
            };
            animation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5))
                .SetInOutCubicEase()
                .SetStoryboard(path, this)
                .AddTo(storyboard);
        }

        public virtual void Initialize()
        { }

        public abstract FeatureAttributeCollection Attributes { get; }

        public bool IsOpen { get; private set; }

        public void Expand()
        {
            if (IsOpen)
            {
                return;
            }
            IsOpen = true;
            animation.To = 0;
            storyboard.Begin();
        }

        public void Collapse()
        {
            if (!IsOpen)
            {
                return;
            }
            IsOpen = false;
            animation.To = ExpandDirection switch
            {
                ExpandDirection.Left or ExpandDirection.Up => ExpandDistance,
                ExpandDirection.Right or ExpandDirection.Down => -ExpandDistance
            };
            storyboard.Begin();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}