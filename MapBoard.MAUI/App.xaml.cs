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

        private void MauiExceptions_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText(Path.Combine(FolderPaths.LogsPath, $"Crash_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                (e.ExceptionObject as Exception).ToString());
            //Toast.Make((e.ExceptionObject as Exception).ToString());
        }
    }
}
