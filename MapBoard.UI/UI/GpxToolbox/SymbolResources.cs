using Esri.ArcGISRuntime.Symbology;
using System.Drawing;

namespace MapBoard.UI.GpxToolbox
{
    public static class SymbolResources
    {
        public static SimpleRenderer NormalRenderer =>
        new SimpleRenderer(NormalLineSymbol);

        public static SimpleRenderer CurrentRenderer =>
            new SimpleRenderer(CurrentLineSymbol);

        public static SimpleRenderer SelectionRenderer =>
            new SimpleRenderer(NotSelectedPointSymbol);

        public static SimpleRenderer BrowsePointRenderer =>
            new SimpleRenderer(BrowsePointSymbol);

        private static SimpleLineSymbol NormalLineSymbol { get; } =
 new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 3);

        private static SimpleLineSymbol CurrentLineSymbol { get; } =
         new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 3);

        public static SimpleMarkerSymbol NotSelectedPointSymbol { get; } = new SimpleMarkerSymbol()
        {
            Color = Color.Blue,
            Size = 3,
            Style = SimpleMarkerSymbolStyle.Circle,
        };

        public static SimpleMarkerSymbol BrowsePointSymbol { get; } = new SimpleMarkerSymbol()
        {
            Color = Color.Blue,
            Size = 12,
            Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2),
            Style = SimpleMarkerSymbolStyle.Circle,
        };

        public static SimpleMarkerSymbol SelectedPointSymbol { get; } = new SimpleMarkerSymbol()
        {
            Color = Color.Red,
            Size = 6,
            Style = SimpleMarkerSymbolStyle.Circle,
        };
    }
}