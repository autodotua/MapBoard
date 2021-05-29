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

        public static Task ShowOkDialogAsync(string title, string message = null)
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

        public static async Task<bool> ShowYesNoDialogAsync(string title, string message = null, string detail = null)
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
                Title = message ?? "错误",
                Message = ex.Message,
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

        public async static Task<int> ShowSelectItemDialogAsync(string title, IEnumerable<DialogItem> items, string extraButtonText = null, Action extraButtonAction = null)
        {
            SelectItemDialog dialog = new SelectItemDialog()
            {
                Title = title,
                Items = new ObservableCollection<DialogItem>(items),
            };
            if (extraButtonText != null)
            {
                dialog.IsShadowEnabled = true;
                dialog.SecondaryButtonText = extraButtonText;
                dialog.SecondaryButtonClick += (p1, p2) => extraButtonAction();
            }
            await dialog.ShowAsync();
            return dialog.SelectedIndex;
        }

        public static CommonDialog CurrentDialog { get; private set; }
        private Task<ContentDialogResult> ShowTask { get; set; }

        /// <summary>
        /// “重写”方法，使弹窗能够依次弹出来而不会报错
        /// </summary>
        /// <returns></returns>
        public async new Task<ContentDialogResult> ShowAsync()
        {
            if (CurrentDialog != null)
            {
                await CurrentDialog.ShowTask;
            }
            CurrentDialog = this;
            ShowTask = base.ShowAsync();
            var result = await ShowTask;
            ShowTask = null;
            CurrentDialog = null;
            return result;
        }
    }
}