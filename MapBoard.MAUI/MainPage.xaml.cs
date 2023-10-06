using Esri.ArcGISRuntime.Mapping;

namespace MapBoard.MAUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            map.Map = new Esri.ArcGISRuntime.Mapping.Map();

            map.Map.Basemap = new Esri.ArcGISRuntime.Mapping.Basemap(new WebTiledLayer(""));
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
           

            //SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
