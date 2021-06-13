using MapBoard.Common;
using ModernWpf.FzExtension.CommonDialog;
using System.Windows.Controls;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// CoordinateTransformationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CoordinateTransformationDialog : CommonDialog
    {
        public CoordinateTransformationDialog()
        {
            InitializeComponent();
        }

        public bool OK { get; private set; } = false;
        public CoordinateSystem Source { get; private set; }

        public CoordinateSystem Target { get; private set; }

        private void GCJ2WGSButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OK = true;
            Source = CoordinateSystem.GCJ02;
            Target = CoordinateSystem.WGS84;
            Hide();
        }

        private void WGS2GCJButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OK = true;
            Source = CoordinateSystem.WGS84;
            Target = CoordinateSystem.GCJ02;
            Hide();
        }
    }
}