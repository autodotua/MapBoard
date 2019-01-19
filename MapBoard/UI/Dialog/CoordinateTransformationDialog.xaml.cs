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
using static MapBoard.IO.CoordinateTransformation;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CoordinateTransformationDialog : Window
    {
        public CoordinateTransformationDialog()
        {
            Owner = Application.Current.MainWindow;
            InitializeComponent();
        }

        public string[] CoordinateSystems => IO.CoordinateTransformation. CoordinateSystems;

        public string SelectedCoordinateSystem1 { get; set; } = "GCJ02";
        public string SelectedCoordinateSystem2 { get; set; }="WGS84";

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!IsLoaded)
            {
                return;
            }
            btn.IsEnabled = SelectedCoordinateSystem1 != SelectedCoordinateSystem2;
        }
    }
}
