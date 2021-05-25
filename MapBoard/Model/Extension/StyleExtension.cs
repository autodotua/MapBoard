using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Model.Extension
{
    public static class StyleExtension
    {
        public static SymbolInfo GetDefaultSymbol(this LayerInfo layer)
        {
            Debug.Assert(layer.Table != null);
            return layer.Table.GeometryType switch
            {
                Esri.ArcGISRuntime.Geometry.GeometryType.Point => SymbolInfo.DefaultPointSymbol,
                Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint => SymbolInfo.DefaultPointSymbol,
                Esri.ArcGISRuntime.Geometry.GeometryType.Polyline => SymbolInfo.DefaultLineSymbol,
                Esri.ArcGISRuntime.Geometry.GeometryType.Polygon => SymbolInfo.DefaultPolygonSymbol,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}