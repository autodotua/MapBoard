using Esri.ArcGISRuntime.Symbology;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.GpxToolbox
{
   public static class SymbolResources
    {
        public static SimpleRenderer GetNormalOverlayRenderer() => new SimpleRenderer(new SimpleMarkerSymbol()
        {
            Color = Color.Red,
            Size = 3,
            Style = SimpleMarkerSymbolStyle.Circle,
        });

        public static SimpleRenderer GetCurrentOverlayRenderer() => new SimpleRenderer(new SimpleMarkerSymbol()
        {
            Color = Color.Blue,
            Size = 3,
            Style = SimpleMarkerSymbolStyle.Circle,
        });


        public static SimpleMarkerSymbol GetSelectedSymbol() => new SimpleMarkerSymbol()
        {
            Color = Color.Green,
            Size = 5,
            Style = SimpleMarkerSymbolStyle.Circle,
        };
    }
}
