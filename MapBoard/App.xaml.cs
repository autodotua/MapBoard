using FzLib.WPF.Dialog;
using MapBoard.Common;
using ModernWpf.Controls;
using ModernWpf.FzExtension;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace MapBoard.Main
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
#if (DEBUG)
            //StartupUri = new Uri("pack://application:,,,/MapBoard.GpxToolbox;component/UI/MainWindow.xaml", UriKind.Absolute);
            //var win = new GpxToolbox.MainWindow();
            //win.Show();
            //win.Loaded += (p1, p2) =>
            //{
            //};
            //return;
#else
            UnhandledException.RegistAll(true);
#endif
            Config.Instance.ThemeChanged += (p1, p2) =>
            {
                Theme.SetTheme(Config.Instance.Theme);
            };
            WindowBase.WindowCreated += (p1, p2) =>
            {
                Theme.SetTheme(Config.Instance.Theme, p1 as Window);
            };
            SnakeBar.DefaultOwner = new WindowOwner(true);
            if (e.Args.Length > 0)
            {
                string arg = e.Args[0];
                StartupUri = arg switch
                {
                    "tile" => new Uri("pack://application:,,,/MapBoard.TileDownloaderSplicer;component/UI/MainWindow.xaml", UriKind.Absolute),
                    "gpx" => new Uri("pack://application:,,,/MapBoard.GpxToolbox;component/UI/MainWindow.xaml", UriKind.Absolute),
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
    }
}