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

namespace MapBoard.Main.UI.Compoment
{
    /// <summary>
    /// ColorChooserTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class ColorPickerTextBox : UserControl
    {
        public ColorPickerTextBox()
        {
            InitializeComponent();
        }

        public SolidColorBrush ColorBrush
        {
            get => GetValue(ColorBrushProperty) as SolidColorBrush;
            set => SetValue(ColorBrushProperty, value);
        }

        public void SetColor(string color)
        {
            ColorBrush = new BrushConverter().ConvertFrom(color) as SolidColorBrush;
        }

        public static DependencyProperty ColorBrushProperty
             = DependencyProperty.Register(nameof(ColorBrush),
                 typeof(SolidColorBrush),
             typeof(ColorPickerTextBox),
             new PropertyMetadata(Brushes.White, new PropertyChangedCallback((o, e) =>
             {
                 if (!(o as ColorPickerTextBox).fromOut)
                 {
                     return;
                 }
                 if (!(o as ColorPickerTextBox).colorPicker.CurrentColor.Equals(e.NewValue as SolidColorBrush))
                 {
                     (o as ColorPickerTextBox).colorPicker.CurrentColor = e.NewValue as SolidColorBrush;
                 }
             })));

        /// <summary>
        /// 用于防止递归赋值
        /// </summary>
        private bool fromOut = true;

        public event EventHandler SelectionColorChanged;

        private void colorPicker_SelectionColorChanged(object sender, EventArgs e)
        {
            fromOut = false;
            ColorBrush = colorPicker.CurrentColor;
            fromOut = true;
            SelectionColorChanged?.Invoke(this, e);
        }

        private void txt_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //ColorBrush = colorPicker.CurrentColor;
            SelectionColorChanged?.Invoke(this, new EventArgs());
        }
    }
}