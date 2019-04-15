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
    public partial class NumberInputDialog : DialogWindowBase
    {
        public NumberInputDialog(string description)
        {
            Description = description;
            InitializeComponent();

        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        public bool Plus { get; set; } = true;
        public bool Integer { get; set; } = false;
        private double number = 5;
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

        private int intNumber = 5;
        public int IntNumber
        {
            get => intNumber;
            set
            {
                if (!Plus || value > 0)
                {
                    intNumber = value;
                }

                Notify(nameof(IntNumber));
            }
        }

        public string Description { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Binding binding = null;
            if (Integer)
            {
                binding = new Binding(nameof(IntNumber));
            }
            else
            {
                binding = new Binding(nameof(Number));
            }
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            txt.SetBinding(TextBox.TextProperty, binding);

            txt.Focus();
            txt.SelectAll();
        }
    }
}
