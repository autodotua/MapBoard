using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MapBoard.UI.Util
{
    public static class FrameworkElementExtension
    {
        public async static Task WaitForLoadedAsync(this FrameworkElement element)
        {
            if (element.IsLoaded)
            {
                return;
            }
            TaskCompletionSource tcs = new TaskCompletionSource();
            element.Loaded += (p1, p2) =>
            {
                tcs.TrySetResult();
            };
            await tcs.Task;
        }
    }

    public class ReverseItemsControlBehavior
    {
        public static DependencyProperty ReverseItemsControlProperty =
            DependencyProperty.RegisterAttached("ReverseItemsControl",
                                                typeof(bool),
                                                typeof(ReverseItemsControlBehavior),
                                                new FrameworkPropertyMetadata(false, OnReverseItemsControlChanged));

        public static bool GetReverseItemsControl(DependencyObject obj)
        {
            return (bool)obj.GetValue(ReverseItemsControlProperty);
        }

        public static void SetReverseItemsControl(DependencyObject obj, object value)
        {
            obj.SetValue(ReverseItemsControlProperty, value);
        }

        private static void OnReverseItemsControlChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                ItemsControl itemsControl = sender as ItemsControl;
                if (itemsControl.IsLoaded == true)
                {
                    DoReverseItemsControl(itemsControl);
                }
                else
                {
                    RoutedEventHandler loadedEventHandler = null;
                    loadedEventHandler = (object sender2, RoutedEventArgs e2) =>
                    {
                        itemsControl.Loaded -= loadedEventHandler;
                        DoReverseItemsControl(itemsControl);
                    };
                    itemsControl.Loaded += loadedEventHandler;
                }
            }
        }

        private static void DoReverseItemsControl(ItemsControl itemsControl)
        {
            Panel itemPanel = GetItemsPanel(itemsControl);
            itemPanel.LayoutTransform = new ScaleTransform(1, -1);
            Style itemContainerStyle;
            if (itemsControl.ItemContainerStyle == null)
            {
                itemContainerStyle = new Style();
            }
            else
            {
                itemContainerStyle = CopyStyle(itemsControl.ItemContainerStyle);
            }
            Setter setter = new Setter();
            setter.Property = ItemsControl.LayoutTransformProperty;
            setter.Value = new ScaleTransform(1, -1);
            itemContainerStyle.Setters.Add(setter);
            itemsControl.ItemContainerStyle = itemContainerStyle;
        }

        private static Panel GetItemsPanel(ItemsControl itemsControl)
        {
            ItemsPresenter itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
            if (itemsPresenter == null)
                return null;
            return GetVisualChild<Panel>(itemsControl);
        }

        private static Style CopyStyle(Style style)
        {
            Style styleCopy = new Style();
            foreach (SetterBase currentSetter in style.Setters)
            {
                styleCopy.Setters.Add(currentSetter);
            }
            foreach (TriggerBase currentTrigger in style.Triggers)
            {
                styleCopy.Triggers.Add(currentTrigger);
            }
            return styleCopy;
        }

        private static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
    }
}