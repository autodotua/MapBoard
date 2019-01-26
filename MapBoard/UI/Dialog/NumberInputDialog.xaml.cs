using FzLib.Control.Dialog;
using FzLib.Control.Extension;
using MapBoard.Main.Style;
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

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class NumberInputDialog : ExtendedWindow
    {
        bool canSelect = false;
        public NumberInputDialog(string title)
        {
            Owner = Application.Current.MainWindow;
            Title = title;
            InitializeComponent();

        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        public bool Plus { get; set; } = true;
        private double number = 0;
        public double Number
        {
            get => number;
            set
            {
                if (!Plus || value > 0)
                {
                    number = value;
                }

                Notify(nameof(Number));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
