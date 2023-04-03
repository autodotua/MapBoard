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
    /// 启动窗口
    /// </summary>
    public partial class SplashWindow : Window
    {
        /// <summary>
        /// 单例
        /// </summary>
        private static SplashWindow instance;

        private SplashWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 是否正在显示
        /// </summary>
        public static bool IsShowing => instance != null;
        /// <summary>
        /// 创建并显示
        /// </summary>
        public static void CreateAndShow()
        {
#if DEBUG
            instance = new SplashWindow();
            instance.Show();
            //DEBUG下，如果运行下面的代码，暂停程序时，执行行就会变成下面的newWindowThread.Start();
#else
            //在新线程中显示SplashWindow，使得SplashWindow的UI不会因为MainWindow的初始化而卡住
            Thread newWindowThread = new Thread(new ThreadStart(() =>
            {
                instance ??= new SplashWindow();
                instance.Show();
                System.Windows.Threading.Dispatcher.Run();
            }));
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();
#endif
        }

        /// <summary>
        /// 关闭或设置为不可见
        /// </summary>
        public static void EnsureInvisible()
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
    }
}