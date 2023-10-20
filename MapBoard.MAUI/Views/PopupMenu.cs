using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Views
{
    public static class PopupMenu
    {
        public static async Task<int> PopupMenuAsync(this View view, IEnumerable<MenuItem> items)
        {
            Popup ppp = new Popup
            {
                Color = Colors.Transparent,
                Anchor = view,
                VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.End,
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
                Margin = new Thickness(8),
                ItemsSource = items,
                ItemTemplate = template,
                WidthRequest = 200,
            };
            list.ItemTapped += (s, e) =>
            {
                ppp.Close(e.ItemIndex);
            };
            Border bd = new Border()
            {
                Content = list,
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
            if(result==null)
            {
                return -1;
            }
            return (int)result;
        }
    }
}
