using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace MapBoard.Main.UI.Map
{
    public class OverlayHelper
    {
        public OverlayHelper()
        {
            Mapview.GraphicsOverlays.Add(headAndTailOverlay);
        }

        private Symbol GetSymbol(string text, Color outlineColor)
        {
            TextSymbol symbol = new TextSymbol()
            {
                Text = text,
                Color = Color.White,
                Size = 16,
                HaloColor = outlineColor,
                HaloWidth = 4,
                OffsetX = 16,
                OffsetY = 16,
                VerticalAlignment = VerticalAlignment.Middle,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            return symbol;
        }

        public ArcMapView Mapview => ArcMapView.Instance;
        private GraphicsOverlay headAndTailOverlay = new GraphicsOverlay();

        public void ClearHeadAndTail()
        {
            headAndTailOverlay.Graphics.Clear();
        }

        public async Task ShowHeadAndTailOfFeatures(IEnumerable<Feature> features)
        {
            headAndTailOverlay.Graphics.Clear();
            int index = 0;
            foreach (var feature in features)
            {
                index++;
                Debug.Assert(feature.FeatureTable.GeometryType == GeometryType.Polyline);
                var line = feature.Geometry as Polyline;
                Debug.Assert(line.Parts.Count == 1);
                var part = line.Parts[0];
                var startPoint = part.StartPoint;
                var endPoint = part.EndPoint;
                Graphic g1 = new Graphic(startPoint) { Symbol = GetSymbol($"折线{index} - 起点", Color.Green) };
                Graphic g2 = new Graphic(endPoint) { Symbol = GetSymbol($"折线{index} - 终点", Color.Red) };
                headAndTailOverlay.Graphics.Add(g1);
                headAndTailOverlay.Graphics.Add(g2);
            }
            await Mapview.ZoomToGeometryAsync(headAndTailOverlay.Extent);
        }
    }
}