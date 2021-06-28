using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MapBoard.UI
{
    /// <summary>
    /// SplashWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SplashWindow : Window
    {
        private SplashWindow()
        {
            InitializeComponent();
        }

        public static bool IsVisiable => instance != null;

        private static SplashWindow instance;

        public static void CreateAndShow()
        {
#if DEBUG
            instance = new SplashWindow();
            instance.Show();
            //DEBUG下，如果运行下面的代码，暂停程序时，执行行就会变成下面的newWindowThread.Start();
#else
            Thread newWindowThread = new Thread(new ThreadStart(() =>
            {
                if (instance == null)
                {
                    instance = new SplashWindow();
                }
                instance.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();
#endif
        }

        public static void EnsureInvisiable()
        {
            if (instance == null)
            {
                return;
            }
            instance.Dispatcher.Invoke(() =>
            {
                instance.Visibility = Visibility.Collapsed;
                instance.Close();
            });
            instance = null;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            DragMove();
        }
    }
}