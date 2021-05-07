using FzLib.Extension;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModernWpf.FzExtension.CommonDialog
{
    public partial class InputDialog : CommonDialog
    {
        private Func<string, bool> Verify { get; }
        private string AllowedCharacters { get; }

        internal InputDialog(Func<string, bool> verify = null, string allowedCharacters = null)
        {
            Verify = verify;
            AllowedCharacters = allowedCharacters;
            InitializeComponent();
        }

        private string message;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        private string inputContent;

        public string InputContent
        {
            get => inputContent;
            set
            {
                if (Verify != null && Verify(value) == false)
                {
                    errorIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    errorIcon.Visibility = Visibility.Collapsed;
                }
                this.SetValueAndNotify(ref inputContent, value, nameof(InputContent));
            }
        }

        private void CommonDialog_PrimaryButtonClick(Controls.ContentDialog sender, Controls.ContentDialogButtonClickEventArgs args)
        {
            if (Verify != null && Verify(InputContent) == false)
            {
                args.Cancel = true;
            }
        }

        private void txt_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (AllowedCharacters != null && e.Text.Any(p => !AllowedCharacters.Contains(p)))
            {
                e.Handled = true;
            }
        }
    }
}