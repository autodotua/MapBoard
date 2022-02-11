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
                    SymbolInfo symbol;
                    var c = feature.Attributes[Parameters.ClassFieldName] as string;
                    if (!string.IsNullOrEmpty(c) && layer.Symbols.ContainsKey(c))
                    {
                        symbol = layer.Symbols[c];
                    }
                    else
                    {
                        symbol = layer.Symbols[""];
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
                    if (layer.Labels!=null&& layer.Labels.Length == 1)
                    {
                        var label = layer.Labels[0];
                        if (string.IsNullOrEmpty(label.CustomLabelExpression) && (label.Info || label.Class || label.Date))
                        {
                            List<string> labels = new List<string>();
                            if (label.Info)
                            {
                                labels.Add(feature.GetAttributeValue(Parameters.LabelFieldName).ToString());
                            }
                            if (label.Class)
                            {
                                labels.Add(feature.GetAttributeValue(Parameters.ClassFieldName).ToString());
                            }
                            if (label.Date && feature.GetAttributeValue(Parameters.DateFieldName) is DateTimeOffset dto)
                            {
                                labels.Add(dto.DateTime.ToString());
                            }
                            placemark.Name = string.Join(label.NewLine ? '\n' : ' ', labels);
                        }
                    }
                    placemark.Description = string.Join('\n', feature.Attributes.Select(p => $"{p.Key}：{p.Value}"));
                    nodes.Add(placemark);
                }
            }
        }
    }
}