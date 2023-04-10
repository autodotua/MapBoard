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
    /// <summary>
    /// 加载错误信息对话框
    /// </summary>
    public partial class ItemsOperaionErrorsDialog : CommonDialog
    {
        public ItemsOperaionErrorsDialog(string title, ICollection<ItemsOperationError> errors)
        {
            Title = title;
            Errors = errors;
            InitializeComponent();
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public ICollection<ItemsOperationError> Errors { get; }

        /// <summary>
        /// 显示对话框
        /// </summary>
        /// <param name="title"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public async static Task TryShowErrorsAsync(string title, ICollection<ItemsOperationError> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }
            await new ItemsOperaionErrorsDialog(title, errors).ShowAsync();
        }
    }
}