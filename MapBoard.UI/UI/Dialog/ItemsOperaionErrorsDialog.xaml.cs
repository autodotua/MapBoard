using FzLib;
using FzLib.WPF.Dialog;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MapBoard.UI.Dialog
{
    public partial class ItemsOperaionErrorsDialog : CommonDialog
    {
        public ItemsOperaionErrorsDialog(string title, ItemsOperationErrorCollection errors)
        {
            Title = title;
            Errors = errors;
            InitializeComponent();
        }

        public ItemsOperationErrorCollection Errors { get; }

        public async static Task TryShowErrorsAsync(string title, ItemsOperationErrorCollection errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }
            await new ItemsOperaionErrorsDialog(title, errors).ShowAsync();
        }
    }
}