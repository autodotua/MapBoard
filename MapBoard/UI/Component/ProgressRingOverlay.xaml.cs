using FzLib.Extension;
using FzLib.UI.Extension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MapBoard.Main.UI.Component
{
    /// <summary>
    /// ProgressRingOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressRingOverlay : UserControl, INotifyPropertyChanged
    {
        public ProgressRingOverlay()
        {
            InitializeComponent();
        }

        private bool isActive;

        public bool IsActive
        {
            get => isActive;
            set => this.SetValueAndNotify(ref isActive, value, nameof(IsActive));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}