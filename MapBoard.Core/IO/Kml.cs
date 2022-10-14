using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.Symbology;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System;
using System.Linq;
using FzLib.Collection;
using Esri.ArcGISRuntime.Mapping;

namespace MapBoard.IO
{
    public static class Kml
    {
        public static async Task ExportAsync(string path, IMapLayerInfo layer)
        {
            KmlDocument kml = new KmlDocument() { Name = layer.Name };
            await Task.Run(async () =>
            {
                await AddToKmlAsync(layer, kml.ChildNodes);
            });
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            await kml.SaveAsAsync(path);
        }

        public static async Task ExportAsync(string path, IEnumerable<IMapLayerInfo> layers)
        {
            KmlDocument kml = new KmlDocument();
            await Task.Run(async () =>
            {
                foreach (var layer in layers)
                {
                    KmlDocument subKml = new KmlDocument() { Name = layer.Name };
                    kml.ChildNodes.Add(subKml);
                    await AddToKmlAsync(layer, subKml.ChildNodes);
                }
            });
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            await kml.SaveAsAsync(path);
        }

        private static async Task AddToKmlAsync(IMapLayerInfo layer, KmlNodeCollection nodes)
        {
            foreach (var feature in await layer.GetAllFeaturesAsync())
            {
                foreach (var g in GeometryUtility.EnsureSinglePart(feature.Geometry))
                {
                    var geometry = new KmlGeometry(g, KmlAltitudeMode.ClampToGround);
                    var placemark = new KmlPlacemark(geometry);
                    foreach (var p in feature.Attributes)
                    {
                        placemark.Attributes.AddOrSetValue(p.Key, p.Value);
                    }
                    placemark.Style = new KmlStyle();
                    SymbolInfo symbol = null;
                    if (layer.Renderer.HasCustomSymbols)
                    {
                        var c = feature.Attributes[layer.Renderer.KeyFieldName].ToString();
                        if (layer.Renderer.Symbols.ContainsKey(c))
                        {
                            symbol = layer.Renderer.Symbols[c];
                        }
                    }
                    if (symbol == null)
                    {
                        symbol = layer.Renderer.DefaultSymbol ?? layer.GetDefaultSymbol();
                    }
                    switch (layer.GeometryType)
                    {
                        case GeometryType.Point:
                            placemark.Style.LabelStyle = new KmlLabelStyle(symbol.FillColor, 1);
                            break;

                        case GeometryType.Polyline:
                            placemark.Style.LineStyle = new KmlLineStyle(symbol.LineColor, symbol.OutlineWidth);
                            break;

                        case GeometryType.Polygon:
                            placemark.Style.PolygonStyle = new KmlPolygonStyle(symbol.FillColor);
                            placemark.Style.PolygonStyle.IsFilled = symbol.FillStyle != (int)SimpleFillSymbolStyle.Null;
                            if (symbol.OutlineWidth > 0)
                            {
                                placemark.Style.PolygonStyle.IsOutlined = true;
                                placemark.Style.LineStyle = new KmlLineStyle(symbol.LineColor, symbol.OutlineWidth);
                            }
                            else
                            {
                                placemark.Style.PolygonStyle.IsOutlined = false;
                            }
                            break;
                    }
                    placemark.Description = string.Join('\n', feature.Attributes.Select(p => $"{p.Key}：{p.Value}"));
                    nodes.Add(placemark);
                }
            }
        }

        public static async Task ImportAsync(string path, MapLayerCollection layers)
        {
            KmlDataset kml = new KmlDataset(new Uri(path));
            await kml.LoadAsync();
            List<MapPoint> points = new List<MapPoint>();
            List<Polyline> lines = new List<Polyline>();
            List<Polygon> polygons = new List<Polygon>();
            foreach (var doc in kml.RootNodes.OfType<KmlDocument>())
            {
                foreach (var node in doc.ChildNodes.OfType<KmlPlacemark>())
                {
                    switch (node.GraphicType)
                    {
                        case KmlGraphicType.None:
                            break;
                        case KmlGraphicType.Point:
                            points.Add(node.Geometry.RemoveZAndM() as MapPoint);
                            break;
                        case KmlGraphicType.Polyline:
                            lines.Add(node.Geometry.RemoveZAndM() as Polyline);
                            break;
                        case KmlGraphicType.Polygon:
                            polygons.Add(node.Geometry.RemoveZAndM() as Polygon);
                            break;
                        case KmlGraphicType.ExtrudedPoint:
                            break;
                        case KmlGraphicType.ExtrudedPolyline:
                            break;
                        case KmlGraphicType.ExtrudedPolygon:
                            break;
                        case KmlGraphicType.Model:
                            break;
                        case KmlGraphicType.MultiGeometry:
                            break;
                    }
                }
            }
            string name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
            if (points.Count > 0)
            {
                var layer = await LayerUtility.CreateShapefileLayerAsync(GeometryType.Point, layers, name: name+"（点）");
                await layer.AddFeaturesAsync(points.Select(p => layer.CreateFeature(null, p)), FeaturesChangedSource.Import);
            }
            if (lines.Count > 0)
            {
                var layer = await LayerUtility.CreateShapefileLayerAsync(GeometryType.Polyline, layers, name: name + "（线）");
                await layer.AddFeaturesAsync(lines.Select(p => layer.CreateFeature(null, p)), FeaturesChangedSource.Import);
            }
            if (polygons.Count > 0)
            {
                var layer = await LayerUtility.CreateShapefileLayerAsync(GeometryType.Polygon, layers, name: name + "（面）");
                await layer.AddFeaturesAsync(polygons.Select(p => layer.CreateFeature(null, p)), FeaturesChangedSource.Import);
            }
        }
    }
}