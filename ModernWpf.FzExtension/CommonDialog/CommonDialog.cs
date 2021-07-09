using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
                Detail = ex == null ? null : ex.ToString(),
                PrimaryButtonText = "确定",
                IsPrimaryButtonEnabled = true,
                Icon = "\uEA39",
                IconBrush = System.Windows.Media.Brushes.Red
            };
            if (ex == null)
            {
                dialog.Title = "错误";
                dialog.Message = message;
            }
            else
            {
                dialog.Title = message ?? "错误";
                dialog.Message = ex.Message;
            }
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

        public async static Task<int> ShowSelectItemDialogAsync(string title, IEnumerable<SelectDialogItem> items, string extraButtonText = null, Action extraButtonAction = null)
        {
            SelectItemDialog dialog = new SelectItemDialog()
            {
                Title = title,
                Items = new List<SelectDialogItem>(items),
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

        public async static Task<IReadOnlyList<CheckDialogItem>> ShowCheckBoxDialogAsync(string title,
            IEnumerable<CheckDialogItem> items,
            bool needAtLeastOneCheck,
            string extraButtonText = null,
            Action extraButtonAction = null)
        {
            CheckBoxDialog dialog = new CheckBoxDialog()
            {
                Title = title,
                Items = new List<CheckDialogItem>(items),
                NeedAtLeastOneCheck = needAtLeastOneCheck
            };
            if (extraButtonText != null)
            {
                dialog.IsShadowEnabled = true;
                dialog.SecondaryButtonText = extraButtonText;
                dialog.SecondaryButtonClick += (p1, p2) => extraButtonAction();
            }
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                return dialog.Items.Where(p => p.IsChecked).ToList().AsReadOnly();
            }
            return null;
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