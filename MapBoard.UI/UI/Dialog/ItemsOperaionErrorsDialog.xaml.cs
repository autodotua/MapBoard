using FzLib.Extension;
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
        public ItemsOperaionErrorsDialog(ItemsOperationErrorCollection errors)
        {
            Errors = errors;
            InitializeComponent();
        }

        public ItemsOperationErrorCollection Errors { get; }

        public async static Task TryShowErrorsAsync(ItemsOperationErrorCollection errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }
            await new ItemsOperaionErrorsDialog(errors).ShowAsync();
        }

        public async static Task TryShowErrorsAsync(Task<ItemsOperationErrorCollection> task)
        {
            var errors = await task;
            if (errors == null || errors.Count == 0)
            {
                return;
            }
            await new ItemsOperaionErrorsDialog(errors).ShowAsync();
        }
    }
}