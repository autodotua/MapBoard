using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ModernWpf.FzExtension.CommonDialog
{
    public abstract class CommonDialog : ContentDialog, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static Task ShowOkDialogAsync(string title, string message)
        {
            DetailTextDialog dialog = new DetailTextDialog()
            {
                Title = title,
                Message = message,
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true
            };
            return dialog.ShowAsync();
        }

        public async static Task<int?> ShowIntInputDialogAsync(string title)
        {
            InputDialog dialog = new InputDialog(p => int.TryParse(p, out int _), "1234567890")
            {
                Title = title,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                return int.Parse(dialog.InputContent);
            }
            return null;
        }

        public async static Task<double?> ShowDoubleInputDialogAsync(string title)
        {
            InputDialog dialog = new InputDialog(p => double.TryParse(p, out double _), "1234567890.")
            {
                Title = title,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                return double.Parse(dialog.InputContent);
            }
            return null;
        }

        public async static Task<string> ShowInputDialogAsync(string title)
        {
            InputDialog dialog = new InputDialog()
            {
                Title = title,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                return dialog.InputContent;
            }
            return null;
        }

        public static async Task<bool> ShowYesNoDialogAsync(string title, string message, string detail = null)
        {
            DetailTextDialog dialog = new DetailTextDialog()
            {
                Title = title,
                Message = message,
                PrimaryButtonText = "是",
                IsPrimaryButtonEnabled = true,
                SecondaryButtonText = "否",
                IsSecondaryButtonEnabled = true,
                Detail = detail
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        public static Task ShowOkDialogAsync(string title, string message, string detail)
        {
            DetailTextDialog dialog = new DetailTextDialog()
            {
                Title = title,
                Message = message,
                Detail = detail,
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true,
            };
            return dialog.ShowAsync();
        }

        public static Task ShowErrorDialogAsync(Exception ex, string message = null)
        {
            DetailTextDialog dialog = new DetailTextDialog()
            {
                Title = "错误",
                Message = message ?? ex.Message,
                Detail = ex == null ? null : ex.ToString(),
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true,
                Icon = "\uEA39",
                IconBrush = System.Windows.Media.Brushes.Red
            };
            return dialog.ShowAsync();
        }

        public static Task ShowErrorDialogAsync(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException();
            }
            return ShowErrorDialogAsync(null, message);
        }

        public async static Task<int> ShowSelectItemDialogAsync(string title, IEnumerable<DialogItem> items)
        {
            SelectItemDialog dialog = new SelectItemDialog()
            {
                Title = title,
                Items = new ObservableCollection<DialogItem>(items)
            };
            await dialog.ShowAsync();
            return dialog.SelectedIndex;
        }
    }
}