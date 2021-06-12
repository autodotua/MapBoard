using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping.Labeling;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using MapBoard.Extension;
using MapBoard.Main.Model;
using MapBoard.Main.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace MapBoard.Main.UI.Map
{
    public class OverlayHelper
    {
        private GraphicsOverlay headAndTailOverlay = new GraphicsOverlay();
        private GraphicsOverlay searchOverlay = new GraphicsOverlay();

        public OverlayHelper(GraphicsOverlayCollection overlays, Func<Geometry, Task> zoomAsync)
        {
            overlays.Add(headAndTailOverlay);
            var d = new LabelInfo().GetLabelDefinition();
            d.Expression = new ArcadeLabelExpression("$feature.Name");
            searchOverlay.LabelDefinitions.Add(d);
            searchOverlay.LabelsEnabled = true;
            overlays.Add(searchOverlay);
            this.zoomAsync = zoomAsync;
        }

        private Symbol GetTextSymbol(string text, Color outlineColor)
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

        /// <summary>
        /// 获取点的符号
        /// </summary>
        /// <param name="level">级别，1最大，3最小，0默认</param>
        /// <returns></returns>
        private Symbol GetPointSymbol(int level = 0)
        {
            return level switch
            {
                1 => new SimpleMarkerSymbol()
                {
                    Color = Color.Orange,
                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2),
                    Size = 12
                },
                2 => new SimpleMarkerSymbol()
                {
                    Color = Color.Orange,
                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2),
                    Size = 8
                },
                3 => new SimpleMarkerSymbol()
                {
                    Color = Color.Orange,
                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 1),
                    Size = 5
                },
                _ => new SimpleMarkerSymbol()
                {
                    Color = Color.Orange,
                    Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2),
                    Size = 8
                },
            };
        }

        private readonly Func<Geometry, Task> zoomAsync;

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
                Graphic g1 = new Graphic(startPoint) { Symbol = GetTextSymbol($"折线{index} - 起点", Color.Green) };
                Graphic g2 = new Graphic(endPoint) { Symbol = GetTextSymbol($"折线{index} - 终点", Color.Red) };
                headAndTailOverlay.Graphics.Add(g1);
                headAndTailOverlay.Graphics.Add(g2);
            }
            await zoomAsync(headAndTailOverlay.Extent);
        }

        /// <summary>
        /// 显示搜索到的POI的位置
        /// </summary>
        /// <param name="pois"></param>
        /// <returns></returns>
        public async Task ShowSearchedPois(IEnumerable<PoiInfo> pois)
        {
            searchOverlay.Graphics.Clear();
            int index = 0;
            foreach (var poi in pois)
            {
                Graphic g = new Graphic(new MapPoint(poi.Longitude, poi.Latitude, SpatialReferences.Wgs84));
                if (index < 3)
                {
                    g.Symbol = GetPointSymbol(1);
                }
                else if (index < 20)
                {
                    g.Symbol = GetPointSymbol(2);
                }
                else
                {
                    g.Symbol = GetPointSymbol(3);
                }
                g.Attributes.Add(nameof(PoiInfo.Name), poi.Name);
                searchOverlay.Graphics.Add(g);
                index++;
            }
            await zoomAsync(searchOverlay.Extent);
        }

        /// <summary>
        /// 清除POI显示
        /// </summary>
        public void ClearSearchedPois()
        {
            searchOverlay.Graphics.Clear();
        }
    }
}