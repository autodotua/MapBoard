using FzLib.Control.Dialog;
using MapBoard.Style;
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
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectStyleDialog : Window
    {
        bool canSelect = false;
        public SelectStyleDialog()
        {
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            var styles = StyleCollection.Instance;
            var list = styles.Styles.Where(p => p.Type == styles.Selected.Type && p!=styles.Selected);
            if (list.Count() > 0)
            {
                cbb.ItemsSource = list;
                cbb.SelectedIndex = 1;
                canSelect = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public StyleInfo SelectedStyle { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(!canSelect)
            {
                SnakeBar.ShowError("没有可选目标");
                Close();
            }
        }
    }
}
