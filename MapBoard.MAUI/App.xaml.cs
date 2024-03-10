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
            Resources["DateTimeFormat"] = Parameters.TimeFormat;
            Resources["DateFormat"] = Parameters.DateFormat;
        }
        private void MauiExceptions_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if(e.ExceptionObject.ToString().Contains("System.Net.Sockets"))
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
