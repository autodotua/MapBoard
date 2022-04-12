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
using System.Xml;
using log4net.Layout;
using System.Reflection;
using System.Diagnostics;
using MapBoard.IO;

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
            InitializeLogs();

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

#if (DEBUG)

#else
            UnhandledException.RegistAll();
            UnhandledException.UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;

#endif
            System.IO.Directory.SetCurrentDirectory(FzLib.Program.App.ProgramDirectoryPath);
            Model.FeatureAttribute.DateFormat = Parameters.DateFormat;
            Model.FeatureAttribute.DateTimeFormat = Parameters.TimeFormat;

            Config.Instance.ThemeChanged += (p1, p2) =>
                Theme.SetTheme(Config.Instance.Theme);
            WindowBase.WindowCreated += (p1, p2) =>
                Theme.SetTheme(Config.Instance.Theme, p1 as Window);
            SnakeBar.DefaultOwner = new WindowOwner(true);

            try
            {
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
            }
            catch (TargetInvocationException ex) when (ex.InnerException?.InnerException?.InnerException?.InnerException is DllNotFoundException)
            {
                SplashWindow.EnsureInvisible();
                Log.Error("找不到C++库", ex);

                var result = MessageBox.Show("C++库不存在，请先安装C++2015-2019或更新版本的x86和x64。" + Environment.NewLine + "是否跳转到下载界面？", "MapBoard", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string url = "https://docs.microsoft.com/zh-cn/cpp/windows/latest-supported-vc-redist";
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                }
                Shutdown();
                return;
            }

            var xcr = Resources.MergedDictionaries.OfType<XamlControlsResources>().FirstOrDefault();
            if (xcr != null)
            {
                xcr.UseCompactResources = true;
            }
        }

        private void InitializeLogs()
        {
            Log = LogManager.GetLogger(GetType());
            RollingFileAppender fa = new RollingFileAppender()
            {
                AppendToFile = true,
                MaxSizeRollBackups = 100,
                StaticLogFileName = true,
                RollingStyle = RollingFileAppender.RollingMode.Date,
                Layout = new PatternLayout("[%date]-[%thread]-[%-p]-[%logger]-[%M] -> %message%newline%newline"),
                File = System.IO.Path.Combine(FolderPaths.LogsPath, "MapBoard.log"),
            };
            fa.ActivateOptions();
            ((log4net.Repository.Hierarchy.Logger)Log.Logger).AddAppender(fa);

            Log.Info("程序启动");
        }

        private void UnhandledException_UnhandledExceptionCatched(object sender, FzLib.Program.Runtime.UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Error("未捕获的异常", e.Exception);
                Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show("程序发生异常，可能出现数据丢失等问题。是否关闭？" + Environment.NewLine + Environment.NewLine + e.Exception.ToString(), "MapBoard - 未捕获的异常", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        Shutdown(-1);
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.Info("程序正常关闭");
        }
    }
}