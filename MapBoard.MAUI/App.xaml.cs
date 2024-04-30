using CommunityToolkit.Maui.Alerts;
using MapBoard.IO;
using MapBoard.Views;

namespace MapBoard
{
    public partial class App : Application
    {
        public App()
        {
            Parameters.AppType = AppType.MAUI;

            MauiExceptions.UnhandledException += MauiExceptions_UnhandledException;

            InitializeComponent();

            MainPage = new MainPage();
        }
        protected override void OnStart()
        {
            base.OnStart();
            Resources["DateTimeFormat"] = "{0:" + Parameters.TimeFormat + "}";
            Resources["DateFormat"] = "{0:" + Parameters.DateFormat + "}";
        }
        private void MauiExceptions_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string strEx = e.ExceptionObject.ToString();
            if (strEx.Contains("System.Net.Sockets")
                || strEx.Contains("Http2Connection")
                || strEx.Contains("Esri.ArcGISRuntime.Http.Caching.CacheManager"))
            {
                //FTP服务存在问题，打开后关闭应用，再次打开就会报错
                return;
            }
            File.WriteAllText(Path.Combine(FolderPaths.LogsPath, $"Crash_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                (e.ExceptionObject as Exception).ToString());
            //Toast.Make((e.ExceptionObject as Exception).ToString());
        }
    }
}
