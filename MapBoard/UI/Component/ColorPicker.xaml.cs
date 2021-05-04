using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MapBoard.UI.Compoment
{
    public partial class ColorPicker : UserControl
    {
        public SolidColorBrush CurrentColor
        {
            get => (SolidColorBrush)GetValue(CurrentColorProperty);
            set
            {
                SetValue(CurrentColorProperty, value);
                SelectionColorChanged?.Invoke(this, new EventArgs());
            }
        }

        public static DependencyProperty CurrentColorProperty =
            DependencyProperty.Register("CurrentColor", typeof(SolidColorBrush), typeof(ColorPicker), new PropertyMetadata(Brushes.Black));

        public static RoutedUICommand SelectColorCommand = new RoutedUICommand("SelectColorCommand", "SelectColorCommand", typeof(ColorPicker));
        private Window _advancedPickerWindow;

        public ColorPicker()
        {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(SelectColorCommand, SelectColorCommandExecute));
        }

        private void SelectColorCommandExecute(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(e.Parameter.ToString()));
        }

        private static void ShowModal(Window advancedColorWindow)
        {
            advancedColorWindow.Owner = Application.Current.MainWindow;
            advancedColorWindow.ShowDialog();
        }

        private void AdvancedPickerPopUpKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                _advancedPickerWindow.Close();
        }

        public event EventHandler SelectionColorChanged;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
            e.Handled = false;
        }

        private void MoreColorsClicked(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
            var advancedColorPickerDialog = new AdvancedColorPickerDialog();
            _advancedPickerWindow = new Window
            {
                Content = advancedColorPickerDialog,
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                WindowState = WindowState.Normal,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Height = 300,
                Width = 232
            };
            _advancedPickerWindow.DragMove();
            _advancedPickerWindow.KeyDown += AdvancedPickerPopUpKeyDown;
            advancedColorPickerDialog.DialogResultEvent += AdvancedColorPickerDialogDialogResultEvent;
            advancedColorPickerDialog.Drag += AdvancedColorPickerDialogDrag;
            ShowModal(_advancedPickerWindow);
        }

        private void AdvancedColorPickerDialogDrag(object sender, DragDeltaEventArgs e)
        {
            _advancedPickerWindow.DragMove();
        }

        private void AdvancedColorPickerDialogDialogResultEvent(object sender, EventArgs e)
        {
            _advancedPickerWindow.Close();
            var dialogEventArgs = (DialogEventArgs)e;
            if (!dialogEventArgs.DialogResult)
                return;
            CurrentColor = dialogEventArgs.SelectedColor;
        }
    }
}