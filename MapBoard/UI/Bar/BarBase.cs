using FzLib.UI.Converter;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MapBoard.Main.UI.Bar
{
    public abstract class BarBase : Grid, INotifyPropertyChanged
    {
        public static double DefaultBarHeight { get; } = 56;
        public virtual double ExpandDistance { get; } = DefaultBarHeight;
        public ArcMapView MapView { get; set; }
        public MapLayerCollection Layers => MapView.Layers;

        protected abstract ExpandDirection ExpandDirection { get; }
        private DoubleAnimation ani;
        private Storyboard storyboard;

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
            ani = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
            ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };
            Storyboard.SetTarget(ani, this);
            switch (ExpandDirection)
            {
                case ExpandDirection.Down:
                case ExpandDirection.Up:
                    Storyboard.SetTargetProperty(ani, new PropertyPath("(Grid.RenderTransform).(TranslateTransform.Y)"));

                    break;

                case ExpandDirection.Left:
                case ExpandDirection.Right:
                    Storyboard.SetTargetProperty(ani, new PropertyPath("(Grid.RenderTransform).(TranslateTransform.X)"));

                    break;
            }
            storyboard = new Storyboard() { Children = { ani } };
        }

        public virtual void Initialize()
        { }

        public abstract FeatureAttributes Attributes { get; }

        public bool IsOpen { get; private set; }

        public void Expand()
        {
            if (IsOpen)
            {
                return;
            }
            IsOpen = true;
            ani.To = 0;
            storyboard.Begin();
        }

        public void Collapse()
        {
            if (!IsOpen)
            {
                return;
            }
            IsOpen = false;
            switch (ExpandDirection)
            {
                case ExpandDirection.Up:
                case ExpandDirection.Left:
                    ani.To = ExpandDistance;

                    break;

                case ExpandDirection.Down:
                case ExpandDirection.Right:
                    ani.To = -ExpandDistance;

                    break;
            }
            storyboard.Begin();
        }

        //protected void Notify(params string[] names)
        //{
        //    foreach (var name in names)
        //    {
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //    }
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        //protected abstract bool CanEdit { get; }

        //protected void MoreAttributesButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (Attributes == null || Attributes.Others.Count == 0)
        //    {
        //        return;
        //    }
        //    Enum2DescriptionConverter c = new Enum2DescriptionConverter();
        //    Border bd = new Border()
        //    {
        //        Padding = new Thickness(8),
        //    };
        //    bd.SetResourceReference(BackgroundProperty, "SystemControlBackgroundChromeMediumBrush");

        //    Grid grd = new Grid();
        //    bd.Child = grd;
        //    grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        //    grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8) });
        //    grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

        //    if (CanEdit)
        //    {
        //        grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(8) });
        //        grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        //    }
        //    int row = 0;
        //    int index = 0;
        //    foreach (var a in Attributes.Others)
        //    {
        //        grd.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        //        grd.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(4) });
        //        TextBlock tbkKey = new TextBlock()
        //        {
        //            Text = a.DisplayName,
        //            VerticalAlignment = VerticalAlignment.Center,
        //            FontWeight = FontWeights.Bold,
        //        };
        //        SetRow(tbkKey, row);

        //        TextBox txt = new TextBox()
        //        {
        //            VerticalAlignment = VerticalAlignment.Center,
        //            MaxWidth = 360,
        //            MaxLines = 5,
        //            TextWrapping = TextWrapping.Wrap
        //        };
        //        string path = null;
        //        switch (a.Type)
        //        {
        //            case FieldInfoType.Integer:
        //                path = nameof(a.IntValue);
        //                break;

        //            case FieldInfoType.Float:
        //                path = nameof(a.FloatValue);
        //                break;

        //            case FieldInfoType.Date:
        //                path = nameof(a.DateValue);
        //                break;

        //            case FieldInfoType.Text:
        //                path = nameof(a.TextValue);
        //                break;
        //        }
        //        Binding binding = new Binding()
        //        {
        //            Path = new PropertyPath($"Attributes.Others[{index}].{path}"),
        //            StringFormat = a.Type == FieldInfoType.Date ? "{0:yyyy-MM-dd}" : null
        //        };
        //        if (!CanEdit)
        //        {
        //            binding.Mode = BindingMode.OneWay;
        //            txt.IsReadOnly = true;
        //            txt.SetResourceReference(BackgroundProperty, "SystemControlBackgroundChromeMediumBrush");
        //            txt.BorderThickness = new Thickness(0);
        //        }
        //        txt.SetBinding(TextBox.TextProperty, binding);

        //        SetRow(txt, row);
        //        SetColumn(txt, 2);

        //        if (CanEdit)
        //        {
        //            TextBlock tbkType = new TextBlock()
        //            {
        //                Text = c.Convert(a.Type, a.Type.GetType(), null, CultureInfo.CurrentCulture) as string,
        //                VerticalAlignment = VerticalAlignment.Center
        //            };
        //            SetRow(tbkType, row);
        //            SetColumn(tbkType, 4);
        //            grd.Children.Add(tbkType);
        //        }

        //        grd.Children.Add(tbkKey);
        //        grd.Children.Add(txt);
        //        row += 2;
        //        index++;
        //    }

        //    if (!CanEdit)
        //    {
        //        for (int i = 1; i < row - 2; i += 2)
        //        {
        //            Border line = new Border()
        //            {
        //                VerticalAlignment = VerticalAlignment.Center,
        //                Height = 2,
        //                HorizontalAlignment = HorizontalAlignment.Stretch
        //            };
        //            line.SetResourceReference(BackgroundProperty, "SystemControlBackgroundBaseMediumLowBrush");
        //            SetColumnSpan(line, 999);
        //            SetRow(line, i);
        //            grd.Children.Add(line);
        //        }
        //    }
        //    Popup p = new Popup()
        //    {
        //        Placement = PlacementMode.Bottom,
        //        PlacementTarget = sender as UIElement,
        //        Child = bd,
        //        AllowsTransparency = true,

        //        StaysOpen = false
        //    };
        //    p.IsOpen = true;
        //}
    }
}