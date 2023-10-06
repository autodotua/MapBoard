namespace MapBoard
{
    public partial class App : Application
    {
        public App()
        {
            Parameters.AppType = AppType.MAUI;
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
