﻿using MapBoard.Main.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MapBoard.Main.UI.OperationBar
{
    public abstract class OperationBarBase : Grid, INotifyPropertyChanged
    {
        public abstract double BarHeight { get; }

        public OperationBarBase() : base()
        {
            DataContext = this;
            Height = BarHeight;
            SetResourceReference(BackgroundProperty, "SystemControlBackgroundAltHighBrush");
            ani = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
            ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };
            Storyboard.SetTarget(ani, this);
            Storyboard.SetTargetProperty(ani, new PropertyPath("(Grid.RenderTransform).(TranslateTransform.Y)"));
            storyboard = new Storyboard() { Children = { ani } };
        }

        private DoubleAnimation ani;
        private Storyboard storyboard;

        public abstract FeatureAttributes Attributes { get; }

        public void Show()
        {
            ani.To = 0;
            storyboard.Begin();
        }

        public void Hide()
        {
            ani.To = -BarHeight;
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

        protected abstract bool CanEdit { get; }

        protected void MoreAttributesButton_Click(object sender, RoutedEventArgs e)
        {
            if (Attributes == null || Attributes.Others.Count == 0)
            {
                return;
            }
            Border bd = new Border()
            {
                Padding = new Thickness(8),
            };
            bd.SetResourceReference(BackgroundProperty, "SystemControlBackgroundChromeMediumBrush");

            Grid grd = new Grid();
            bd.Child = grd;
            grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8) });
            grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            var field = LayerCollection.Instance.Selected.Fields;
            int row = 0;
            int index = 0;
            foreach (var a in Attributes.Others)
            {
                grd.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                grd.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(4) });
                TextBlock tbkKey = new TextBlock()
                {
                    Text = a.DisplayName,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                };
                SetRow(tbkKey, row);

                TextBox txt = new TextBox()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    MaxWidth = 360,
                    MaxLines = 5,
                    TextWrapping = TextWrapping.Wrap
                };
                Binding binding = new Binding()
                {
                    Path = new PropertyPath($"Attributes.Others[{index}].Value"),
                };
                binding.ValidationRules.Add(new ExceptionValidationRule());
                if (a.Type == FieldInfoType.Date)
                {
                    binding.StringFormat = "{0:yyyy-MM-dd}";
                }
                if (!CanEdit)
                {
                    binding.Mode = BindingMode.OneWay;
                    txt.IsReadOnly = true;
                    txt.SetResourceReference(BackgroundProperty, "SystemControlBackgroundChromeMediumBrush");
                    txt.BorderThickness = new Thickness(0);
                }
                txt.SetBinding(TextBox.TextProperty, binding);

                SetRow(txt, row);
                SetColumn(txt, 2);
                grd.Children.Add(tbkKey);
                grd.Children.Add(txt);
                row += 2;
                index++;
            }

            for (int i = 1; i < row - 2; i += 2)
            {
                Border line = new Border()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Height = 2,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                line.SetResourceReference(BackgroundProperty, "SystemControlBackgroundBaseMediumLowBrush");
                SetColumnSpan(line, 999);
                SetRow(line, i);
                grd.Children.Add(line);
            }
            Popup p = new Popup()
            {
                Placement = PlacementMode.Bottom,
                PlacementTarget = sender as UIElement,
                Child = bd,
                AllowsTransparency = true,

                StaysOpen = false
            };
            p.IsOpen = true;
        }
    }
}