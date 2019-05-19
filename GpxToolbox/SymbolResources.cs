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
        public static SimpleRenderer NormalPointRenderer => null;
        //    new SimpleRenderer(new SimpleMarkerSymbol()
        //{
        //    Color = Color.Red,
        //    Size = 0,
        //    Style = SimpleMarkerSymbolStyle.Circle,
        //});
        public static SimpleLineSymbol NormalLineSymbol =>
 new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 3);

        public static SimpleRenderer CurrentPointRenderer => new SimpleRenderer(new SimpleMarkerSymbol()
        {
            Color = Color.Red,
            Size = 3,
            Style = SimpleMarkerSymbolStyle.Circle,
        });


        public static SimpleLineSymbol CurrentLineSymbol => 
         new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 3);

        public static SimpleMarkerSymbol SelectedPointSymbol=> new SimpleMarkerSymbol()
        {
            Color = Color.Red,
            Size = 6,
            Style = SimpleMarkerSymbolStyle.Circle,
        };
    }
}
