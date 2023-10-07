using MapBoard.Pages;

namespace MapBoard
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("LayerPage", typeof(LayerPage));
            if (true||DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                CurrentItem = tab;
            }
        }
    }
}
