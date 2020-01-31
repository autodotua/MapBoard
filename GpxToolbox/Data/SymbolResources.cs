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
        public static SimpleRenderer NormalRenderer=>
        new SimpleRenderer(NormalLineSymbol);
        public static SimpleRenderer CurrentRenderer=>
            new SimpleRenderer(CurrentLineSymbol);
        public static SimpleRenderer SelectionRenderer=>
            new SimpleRenderer(NotSelectedPointSymbol);

 //       public static SimpleRenderer NormalPointRenderer => null;
 //       //    new SimpleRenderer(new SimpleMarkerSymbol()
 //       //{
 //       //    Color = Color.Red,
 //       //    Size = 0,
 //       //    Style = SimpleMarkerSymbolStyle.Circle,
 //       //});
        private static SimpleLineSymbol NormalLineSymbol { get; }=
 new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 3);

        //       public static SimpleRenderer CurrentPointRenderer => new SimpleRenderer(new SimpleMarkerSymbol()
        //       {
        //           Color = Color.Red,
        //           Size = 3,
        //           Style = SimpleMarkerSymbolStyle.Circle,
        //       });


        private static SimpleLineSymbol CurrentLineSymbol { get; }=
         new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 3);

        public static SimpleMarkerSymbol NotSelectedPointSymbol { get; }= new SimpleMarkerSymbol()
        {
            Color = Color.Blue,
            Size = 3,
            Style = SimpleMarkerSymbolStyle.Circle,
        };  public static SimpleMarkerSymbol SelectedPointSymbol { get; }= new SimpleMarkerSymbol()
        {
            Color = Color.Red,
            Size = 6,
            Style = SimpleMarkerSymbolStyle.Circle,
        };
    }
}
