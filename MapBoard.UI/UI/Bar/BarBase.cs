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
    /// <summary>
    /// 位于地图上方或右侧的条状操作面板
    /// </summary>
    public abstract class BarBase : Grid, INotifyPropertyChanged
    {
        /// <summary>
        /// 展开动画
        /// </summary>
        private readonly DoubleAnimation animation;

        /// <summary>
        /// 展开动画板
        /// </summary>
        private readonly Storyboard storyboard = new Storyboard();

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
                _ => throw new InvalidEnumArgumentException()
            };
            animation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5))
                .SetInOutCubicEase()
                .SetStoryboard(path, this)
                .AddTo(storyboard);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 默认的伸出距离
        /// </summary>
        public static double DefaultBarDistance { get; } = 56;

        /// <summary>
        /// 要素属性
        /// </summary>
        public abstract FeatureAttributeCollection Attributes { get; }

        /// <summary>
        /// 展开距离
        /// </summary>
        public virtual double ExpandDistance { get; } = DefaultBarDistance;

        /// <summary>
        /// 是否已经被展开
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 图层
        /// </summary>
        public MapLayerCollection Layers => MapView?.Layers;

        /// <summary>
        /// 地图
        /// </summary>
        public MainMapView MapView { get; set; }

        /// <summary>
        /// 展开方向
        /// </summary>
        protected abstract ExpandDirection ExpandDirection { get; }

        /// <summary>
        /// 收拢
        /// </summary>
        /// <exception cref="InvalidEnumArgumentException"></exception>
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
                ExpandDirection.Right or ExpandDirection.Down => -ExpandDistance,
                _ => throw new InvalidEnumArgumentException()
            };
            storyboard.Begin();
        }

        /// <summary>
        /// 展开
        /// </summary>
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

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize()
        { }
    }
}