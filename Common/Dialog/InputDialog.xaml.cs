using FzLib.Control.Dialog;
using FzLib.Control.Extension;
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

namespace MapBoard.Common.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InputDialog : DialogWindowBase
    {
        public InputDialog(string description)
        {
            Description = description;
            InitializeComponent();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public string Text { get; set; }

        public string Description { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txt.Focus();
            txt.SelectAll();
        }
    }
}
