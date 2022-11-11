using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Labeling;
using Esri.ArcGISRuntime.Symbology;
using MapBoard.Model;
using MapBoard.Mapping;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBoard.Mapping.Model;
using System;
using System.Diagnostics;

namespace MapBoard.Util
{
    public static class StyleUtility
    {
        public static void ApplyStyle(this IMapLayerInfo layer)
        {
            layer.ApplyRenderer();
            layer.ApplyLabel();
        }

        public static LabelDefinition GetLabelDefinition(this LabelInfo label)
        {
            var exp = new ArcadeLabelExpression(label.Expression);
            TextSymbol symbol = new TextSymbol()
            {
                HaloColor = label.HaloColor,
                Color = label.FontColor,
                BackgroundColor = label.BackgroundColor,
                Size = label.FontSize,
                HaloWidth = label.HaloWidth,
                OutlineWidth = label.OutlineWidth,
                OutlineColor = label.OutlineColor,
                FontWeight = label.Bold ? FontWeight.Bold : FontWeight.Normal,
                FontStyle = label.Italic ? FontStyle.Italic : FontStyle.Normal,
                FontFamily = string.IsNullOrWhiteSpace(label.FontFamily) ? null : label.FontFamily
            };
            LabelDefinition labelDefinition = new LabelDefinition(exp, symbol)
            {
                WhereClause = label.WhereClause,
                MinScale = label.MinScale,
                TextLayout = (LabelTextLayout)label.Layout,
                RepeatStrategy = label.AllowRepeat ? LabelRepeatStrategy.Repeat : LabelRepeatStrategy.None,
                LabelOverlapStrategy = label.AllowOverlap ? LabelOverlapStrategy.Allow : LabelOverlapStrategy.Exclude,
                FeatureInteriorOverlapStrategy = label.AllowOverlap ? LabelOverlapStrategy.Allow : LabelOverlapStrategy.Exclude,
                FeatureBoundaryOverlapStrategy = label.AllowOverlap ? LabelOverlapStrategy.Allow : LabelOverlapStrategy.Exclude,
                DeconflictionStrategy = (LabelDeconflictionStrategy)label.DeconflictionStrategy,
                RepeatDistance = label.RepeatDistance,
            };
            return labelDefinition;
        }

        public static Symbol ToSymbol(this SymbolInfo symbolInfo, GeometryType geometryType)
        {
            Symbol symbol = null;

            switch (geometryType)
            {
                case GeometryType.Point:
                case GeometryType.Multipoint:
                    var outline = new SimpleLineSymbol((SimpleLineSymbolStyle)symbolInfo.LineStyle, symbolInfo.LineColor, symbolInfo.OutlineWidth);
                    symbol = new SimpleMarkerSymbol((SimpleMarkerSymbolStyle)symbolInfo.PointStyle, symbolInfo.FillColor, symbolInfo.Size)
                    {
                        Outline = outline
                    };

                    break;

                case GeometryType.Polyline:
                    symbol = new SimpleLineSymbol((SimpleLineSymbolStyle)symbolInfo.LineStyle, symbolInfo.LineColor, symbolInfo.OutlineWidth);
                    if (symbolInfo.Arrow > 0)
                    {
                        (symbol as SimpleLineSymbol).MarkerPlacement = (SimpleLineSymbolMarkerPlacement)(symbolInfo.Arrow - 1);
                        (symbol as SimpleLineSymbol).MarkerStyle = SimpleLineSymbolMarkerStyle.Arrow;
                    }
                    break;

                case GeometryType.Polygon:
                    var lineSymbol = new SimpleLineSymbol((SimpleLineSymbolStyle)symbolInfo.LineStyle, symbolInfo.LineColor, symbolInfo.OutlineWidth);
                    symbol = new SimpleFillSymbol((SimpleFillSymbolStyle)symbolInfo.FillStyle, symbolInfo.FillColor, lineSymbol);
                    break;
            }
            return symbol;
        }

        private static void ApplyLabel(this IMapLayerInfo layer)
        {
            layer.Layer.LabelDefinitions.Clear();
            if (layer.Labels == null)
            {
                return;
            }
            foreach (var label in layer.Labels)
            {
                layer.Layer.LabelDefinitions.Add(label.GetLabelDefinition());
            }
            layer.Layer.LabelsEnabled = layer.Labels.Length > 0;
        }

        private static void ApplyRenderer(this IMapLayerInfo layer)
        {
            switch (layer.Type)
            {
                case MapLayerInfo.Types.Shapefile:
                case MapLayerInfo.Types.Temp:
                case null:
                    {
                        UniqueValueRenderer renderer = new UniqueValueRenderer();
                        if (layer.Renderer.HasCustomSymbols)
                        {
                            renderer.FieldNames.Add(layer.Renderer.KeyFieldName);

                            foreach (var info in layer.Renderer.Symbols)
                            {
                                try
                                {
                                    Func<object> func = layer.Fields.First(p => p.Name == layer.Renderer.KeyFieldName).Type switch
                                    {
                                        FieldInfoType.Text or FieldInfoType.Time => () => info.Key,
                                        FieldInfoType.Date => () => DateTime.Parse(info.Key),
                                        FieldInfoType.Integer => () => int.Parse(info.Key),
                                        FieldInfoType.Float => () => double.Parse(info.Key),
                                        _ => throw new NotImplementedException(),
                                    };
                                    renderer.UniqueValues.Add(new UniqueValue(info.Key, info.Key, info.Value.ToSymbol(layer.GeometryType), func()));

                                }
                                catch(Exception ex)
                                {
                                    Debug.WriteLine("转换唯一值渲染器的Key失败："+ex.ToString());
                                }

                            }
                        }
                        renderer.DefaultSymbol = (layer.Renderer.DefaultSymbol ?? layer.GetDefaultSymbol()).ToSymbol(layer.GeometryType);
                        layer.Layer.Renderer = renderer;
                    }
                    break;

                case MapLayerInfo.Types.WFS:
                    layer.Layer.Renderer = new SimpleRenderer((layer.Renderer.DefaultSymbol ?? layer.GetDefaultSymbol()).ToSymbol(layer.GeometryType));
                    break;

                default:
                    break;
            }
        }
    }
}