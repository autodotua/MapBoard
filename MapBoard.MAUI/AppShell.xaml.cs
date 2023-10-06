namespace MapBoard
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            if (true||DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                CurrentItem = tab;
            }
        }
    }
}
