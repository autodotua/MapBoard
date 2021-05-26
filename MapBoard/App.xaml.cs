using FzLib.Program.Runtime;
using FzLib.WPF.Dialog;
using MapBoard.Common;
using ModernWpf.Controls;
using ModernWpf.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
            UnhandledException.RegistAll();
            UnhandledException.UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;

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

        private async void UnhandledException_UnhandledExceptionCatched(object sender, FzLib.Program.Runtime.UnhandledExceptionEventArgs e)
        {
            //await Task.Run(() =>
            //{
            //    try
            //    {
            //        LogUtility.AddLog(e.Exception.Message, e.Exception.ToString());
            //    }
            //    catch (Exception ex)
            //    {
            //    }
            //});
            if (!e.Exception.Source.StartsWith("Microsoft.EntityFrameworkCore"))
            {
                await Dispatcher.Invoke(async () =>
                {
                    try
                    {
                        await CommonDialog.ShowErrorDialogAsync(e.Exception, "程序出现异常");
                    }
                    catch
                    {
                        try
                        {
                            MessageBox.Show(e.Exception.ToString());
                        }
                        catch
                        {
                        }
                    }
                });
            }
        }
    }
}