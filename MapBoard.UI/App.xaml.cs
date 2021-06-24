﻿using FzLib.Program.Runtime;
using FzLib.WPF.Dialog;
using ModernWpf.Controls;
using ModernWpf.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using MapBoard.UI;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ModernWpf.Controls.Primitives;
using System.Windows.Media;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace MapBoard
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static ILog Log { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Log = LogManager.GetLogger(GetType());
            Log.Info("程序启动");

#if (DEBUG)
            //StartupUri = new Uri("pack://application:,,,/MapBoard.GpxToolbox;component/UI/MainWindow.xaml", UriKind.Absolute);
            //var win = new GpxToolbox.MainWindow();
            //win.Show();
            //win.Loaded += (p1, p2) =>
            //{
            //};
            //return;
#else
            UnhandledException.RegistAll();
            UnhandledException.UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;

#endif
            SplashWindow.CreateAndShow();

            Model.FeatureAttribute.DateFormat = Parameters.DateFormat;
            Model.FeatureAttribute.DateTimeFormat = Parameters.TimeFormat;

            Config.Instance.ThemeChanged += (p1, p2) =>
                Theme.SetTheme(Config.Instance.Theme);
            WindowBase.WindowCreated += (p1, p2) =>
                Theme.SetTheme(Config.Instance.Theme, p1 as Window);
            SnakeBar.DefaultOwner = new WindowOwner(true);

            if (e.Args.Length > 0)
            {
                string arg = e.Args[0];
                StartupUri = arg switch
                {
                    "tile" => new Uri("UI/TileDownloader/TileDownloaderWindow.xaml", UriKind.Relative),
                    "gpx" => new Uri("UI/GpxToolbox/GpxWindow.xaml", UriKind.Relative),
                    _ => new Uri("UI/MainWindow.xaml", UriKind.Relative),
                };
            }
            else
            {
                StartupUri = new Uri("UI/MainWindow.xaml", UriKind.Relative);
            }

            var xcr = Resources.MergedDictionaries.OfType<XamlControlsResources>().FirstOrDefault();
            if (xcr != null)
            {
                xcr.UseCompactResources = true;
            }
        }

        private void UnhandledException_UnhandledExceptionCatched(object sender, FzLib.Program.Runtime.UnhandledExceptionEventArgs e)
        {
            Log.Error("未捕获的异常", e.Exception);
            MessageBox.Show(e.Exception.ToString(), "MapBoard - 未捕获的异常", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.Info("程序正常关闭");
        }
    }
}