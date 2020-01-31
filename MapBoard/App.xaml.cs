using FzLib.Program.Runtime;
using FzLib.UI.Program;
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
        }

    }
}
