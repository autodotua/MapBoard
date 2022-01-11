using FzLib.Program.Runtime;
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
using MapBoard.UI.GpxToolbox;
using MapBoard.UI.TileDownloader;
using log4net.Appender;
using MapBoard.Util;
using Microsoft.WindowsAPICodePack.FzExtension;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace MapBoard
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            try
            {
                SplashWindow.CreateAndShow();
            }
            catch (Exception ex)
            {
            }
        }

        public static ILog Log { get; private set; }
        private SingleInstance singleInstance;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            Log = LogManager.GetLogger(GetType());
            var h = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
            foreach (IAppender a in h.Root.Appenders)
            {
                if (a is FileAppender)
                {
                    FileAppender fa = (FileAppender)a;
                    fa.File = System.IO.Path.Combine(Parameters.LogsPath, "MapBoard.log");
                    fa.ActivateOptions();
                    break;
                }
            }

            singleInstance = new SingleInstance(FzLib.Program.App.ProgramName);
            await singleInstance.CheckAndOpenWindow<MainWindow>(this);
            if (singleInstance.ExistAnotherInstance)
            {
                Log.Info("已存在正在运行的程序");
                //过于复杂，不实现
                //if (e.Args.Length > 0)
                //{
                //    if (e.Args[0].EndsWith("mpmpkg"))
                //    {
                //        Log.Info("通知正在运行的程序打开地图包");
                //        try
                //        {
                //            await PipeHelper.Send("mbmpkg " + e.Args[0]);
                //        }
                //        catch(Exception ex)
                //        {
                //            Log.Error("通知正在运行的程序打开地图包失败",ex);
                //        }
                //    }
                //}
                Environment.Exit(0);
                return;
            }
            Log.Info("程序启动");

#if (DEBUG)

#else
            UnhandledException.RegistAll();
            UnhandledException.UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;

#endif

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
                MainWindow = arg switch
                {
                    "tile" => await MainWindowBase.CreateAndShowAsync<TileDownloaderWindow>(),
                    "gpx" => await MainWindowBase.CreateAndShowAsync<GpxWindow>(),
                    string s when s.EndsWith(".mbmpkg") => await MainWindowBase.CreateAndShowAsync<MainWindow>(w => w.LoadFile = arg),
                    string s when s.EndsWith(".gpx") => await MainWindowBase.CreateAndShowAsync<GpxWindow>(w => w.LoadFiles = new[] { arg }),
                    _ => await MainWindowBase.CreateAndShowAsync<MainWindow>(),
                };
            }
            else
            {
                MainWindow = await MainWindowBase.CreateAndShowAsync<MainWindow>();
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