using FzLib.Program.Runtime;
using MapBoard.Main.UI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static FzLib.IO.Shortcut;

namespace MapBoard.Main
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
#if (!DEBUG)
            UnhandledException.RegistAll(true);
#endif
            if (e.Args.Length > 0)
            {
                string arg = e.Args[0];
                switch (arg)
                {
                    case "tile":
                        StartupUri = new Uri("pack://application:,,,/MapBoard.TileDownloaderSplicer;component/MainWindow.xaml", UriKind.Absolute);
                        break;
                    case "gpx":
                        StartupUri = new Uri("pack://application:,,,/MapBoard.GpxToolbox;component/MainWindow.xaml", UriKind.Absolute);
                        break;
                    default:
                        StartupUri = new Uri("UI/MainWindow.xaml", UriKind.Relative);
                        break;
                }
            }
            else
            {
                StartupUri = new Uri("UI/MainWindow.xaml", UriKind.Relative);
            }
        }

    }
}
