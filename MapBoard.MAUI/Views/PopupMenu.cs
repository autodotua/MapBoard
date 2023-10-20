using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace MapBoard.Views
{
    public static class PopupMenu
    {
        public static async Task<int> PopupMenuAsync(this View view, IEnumerable<MenuItem> items, string title = null)
        {
            Popup ppp = new Popup
            {
                Color = Colors.Transparent,
                Anchor = view,
                CanBeDismissedByTappingOutsideOfPopup = true,
            };
            var template = new DataTemplate(() =>
            {
                TextCell tc = new TextCell();
                tc.SetBinding(TextCell.TextProperty, new Binding(nameof(MenuItem.Text)));
                tc.SetBinding(TextCell.IsEnabledProperty, new Binding(nameof(MenuItem.IsEnabled)));
                tc.SetAppThemeColor(TextCell.TextColorProperty, Colors.Black, Colors.White);
                return tc;
            });
            ListView list = new ListView()
            {
                BackgroundColor = Colors.Transparent,
                SelectionMode = ListViewSelectionMode.None,
                HorizontalOptions = LayoutOptions.Fill,
                ItemsSource = items,
                ItemTemplate = template,
                WidthRequest = 200,
            };
            list.ItemTapped += (s, e) =>
            {
                ppp.Close(e.ItemIndex);
            };
            Grid grid = new Grid()
            {
                Margin = new Thickness(8),
            };
            grid.Add(list);
            if (title != null)
            {
                Label titleLabel = new Label()
                {
                    Text = title,
                    FontSize = 24,
                    Margin=new Thickness(8,0,0,0),
                    TextColor = Colors.Gray
                };
                grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
                grid.AddRowDefinition(new RowDefinition(8));
                grid.AddRowDefinition(new RowDefinition(new GridLength(1, GridUnitType.Star)));
                grid.Add(titleLabel);
                Grid.SetRow(list, 2);
            }
            Border bd = new Border()
            {
                Content = grid,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle()
                {
                    CornerRadius = new CornerRadius(4)
                },
                Shadow = new Shadow()
                {
                    Opacity = 0.6f
                },
            };
            bd.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Colors.Black);
            ppp.Content = bd;
            var result = await MainPage.Current.ShowPopupAsync(ppp);
            if (result == null)
            {
                return -1;
            }
            return (int)result;
        }
    }
}
