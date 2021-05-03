using System.Windows;

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