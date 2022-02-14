using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping.Labeling;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.Extension;
using MapBoard.Model;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    public class OverlayHelper
    {
        private GraphicsOverlay headAndTailOverlay = new GraphicsOverlay();
        private GraphicsOverlay poiOverlay = new GraphicsOverlay();
        private GraphicsOverlay routeOverlay = new GraphicsOverlay();
        private GraphicsOverlay locationInfoOverlay = new GraphicsOverlay();
        private GraphicsOverlay drawOverlay = new GraphicsOverlay();

        public OverlayHelper(GraphicsOverlayCollection overlays, Func<Geometry, Task> zoomAsync)
        {
            overlays.Add(headAndTailOverlay);
            overlays.Add(locationInfoOverlay);

            var d = new LabelInfo().GetLabelDefinition();
            d.Expression = new ArcadeLabelExpression("$feature.Name");
            poiOverlay.LabelDefinitions.Add(d);
            poiOverlay.LabelsEnabled = true;
            overlays.Add(poiOverlay);

            d = new LabelInfo().GetLabelDefinition();
            d.Expression = new ArcadeLabelExpression("$feature.Name");
            d.WhereClause = "Distance is null";
            routeOverlay.LabelDefinitions.Add(d);
            d = new LabelInfo().GetLabelDefinition();
            d.Expression = new ArcadeLabelExpression(@"$feature.Name +'\n'+ $feature.Distance + 'm\n' + $feature.Duration");
            d.WhereClause = "Distance is not null";
            routeOverlay.LabelDefinitions.Add(d);
            routeOverlay.LabelsEnabled = true;
            overlays.Add(routeOverlay);

            drawOverlay.Renderer = new SimpleRenderer();
            overlays.Add(drawOverlay);

            this.zoomAsync = zoomAsync;
        }

        private readonly Func<Geometry, Task> zoomAsync;

        #region 样式

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

        #endregion 样式

        #region 折线端点

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

        #endregion 折线端点

        #region POI

        internal void SelectPoi(PoiInfo poi)
        {
            poiOverlay.ClearSelection();
            if (poi != null)
            {
                if (poi2Graphic.ContainsKey(poi))
                {
                    poi2Graphic[poi].IsSelected = true;
                    zoomAsync(poi2Graphic[poi].Geometry.Extent);
                }
            }
        }

        public Dictionary<PoiInfo, Graphic> poi2Graphic = new Dictionary<PoiInfo, Graphic>();

        /// <summary>
        /// 显示搜索到的POI的位置
        /// </summary>
        /// <param name="pois"></param>
        /// <returns></returns>
        public async Task ShowPois(IEnumerable<PoiInfo> pois)
        {
            poiOverlay.Graphics.Clear();
            if (!pois.Any())
            {
                return;
            }
            int index = 0;
            foreach (var poi in pois)
            {
                Graphic g = new Graphic(poi.Location.ToMapPoint());
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
                poiOverlay.Graphics.Add(g);
                poi2Graphic.Add(poi, g);
                index++;
            }
            await zoomAsync(poiOverlay.Extent);
        }

        /// <summary>
        /// 清除POI显示
        /// </summary>
        public void ClearPois()
        {
            poiOverlay.Graphics.Clear();
        }

        #endregion POI

        #region 路径

        public Dictionary<RouteStepInfo, Graphic> step2Graphic = new Dictionary<RouteStepInfo, Graphic>();

        public async Task ShowRoutes(IEnumerable<RouteInfo> routes)
        {
            foreach (var graphic in routeOverlay.Graphics.Where(p => p.Geometry is Polyline).ToList())
            {
                routeOverlay.Graphics.Remove(graphic);
            }
            step2Graphic.Clear();
            Color[] colors = new Color[]
            {
                Color.Red,
                Color.Orange,
                Color.Blue,
                Color.Purple
            };
            int index = 0;
            foreach (var route in routes)
            {
                foreach (var step in route.Steps)
                {
                    Polyline line = new Polyline(step.Locations.Select(p => p.ToMapPoint()));
                    Graphic g = new Graphic(line);
                    g.Attributes.Add("Name", step.Road);
                    g.Attributes.Add("Distance", step.Distance);
                    g.Attributes.Add("Duration", step.Duration.ToString());
                    g.Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, colors[index % colors.Length], 4);
                    routeOverlay.Graphics.Add(g);
                    step2Graphic.Add(step, g);
                }
                index++;
            }
            await zoomAsync(routeOverlay.Extent);
        }

        public void SelectStep(RouteStepInfo step)
        {
            routeOverlay.ClearSelection();
            if (step != null)
            {
                if (step2Graphic.ContainsKey(step))
                {
                    step2Graphic[step].IsSelected = true;
                    zoomAsync(step2Graphic[step].Geometry.Extent);
                }
            }
        }

        public void SelectRoute(RouteInfo route)
        {
            routeOverlay.ClearSelection();
            if (route != null)
            {
                foreach (var step in route.Steps)
                {
                    if (step2Graphic.ContainsKey(step))
                    {
                        step2Graphic[step].IsSelected = true;
                    }
                }
            }
        }

        public void SetRouteOrigin(MapPoint point)
        {
            Graphic graphic = routeOverlay.Graphics.Where(p => p.Geometry is MapPoint)
                .FirstOrDefault(p => p.Attributes["Name"].Equals("起点"));
            if (graphic != null)
            {
                routeOverlay.Graphics.Remove(graphic);
            }
            graphic = new Graphic(point);
            graphic.Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, Color.Orange, 8)
            { Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2) };
            graphic.Attributes.Add("Name", "起点");
            routeOverlay.Graphics.Add(graphic);
        }

        public void SetRouteDestination(MapPoint point)
        {
            Graphic graphic = routeOverlay.Graphics.Where(p => p.Geometry is MapPoint)
                  .FirstOrDefault(p => p.Attributes["Name"].Equals("终点"));
            if (graphic != null)
            {
                routeOverlay.Graphics.Remove(graphic);
            }
            graphic = new Graphic(point);
            graphic.Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.Blue, 8)
            { Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2) };
            graphic.Attributes.Add("Name", "终点");
            routeOverlay.Graphics.Add(graphic);
        }

        public void ClearRoutes()
        {
            routeOverlay.Graphics.Clear();
        }

        #endregion 路径

        #region 地理逆编码

        public void ShowLocation(MapPoint point)
        {
            locationInfoOverlay.ClearSelection();
            Graphic g = new Graphic(point);
            g.Symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.Orange, 24)
            { Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2) };
            locationInfoOverlay.Graphics.Add(g);
        }

        public void ClearLocation()
        {
            locationInfoOverlay.Graphics.Clear();
        }

        #endregion 地理逆编码

        public void SetNearestVertexPoint(MapPoint point)
        {
            if (drawOverlay.Graphics.Any(p => p.Attributes["Type"].Equals(1)))
            {
                drawOverlay.Graphics.Remove(drawOverlay.Graphics.First(p => p.Attributes["Type"].Equals(1)));
            }
            if (point != null)
            {
                var g = new Graphic(point, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, Color.Red, 20));
                g.Attributes.Add("Type", 1);
                drawOverlay.Graphics.Add(g);
            }
        }

        public void SetNearestPointPoint(MapPoint point)
        {
            if (drawOverlay.Graphics.Any(p => p.Attributes["Type"].Equals(2)))
            {
                drawOverlay.Graphics.Remove(drawOverlay.Graphics.First(p => p.Attributes["Type"].Equals(2)));
            }
            if (point != null)
            {
                var g = new Graphic(point, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, Color.Yellow, 20));
                g.Attributes.Add("Type", 2);
                drawOverlay.Graphics.Add(g);
            }
        }
    }
}