﻿using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Labeling;
using Esri.ArcGISRuntime.Symbology;
using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;

namespace MapBoard.Util
{
    public static class StyleUtility
    {
        public static void ApplyStyle(this MapLayerInfo layer)
        {
            layer.ApplyRenderer();
            layer.ApplyLabel();
        }

        private static void ApplyRenderer(this MapLayerInfo layer)
        {
            UniqueValueRenderer renderer = new UniqueValueRenderer();
            renderer.FieldNames.Add(Parameters.ClassFieldName);
            if (layer.Symbols.Count == 0)
            {
                layer.Symbols.Add("", layer.GetDefaultSymbol());
            }
            foreach (var info in layer.Symbols)
            {
                var key = info.Key;
                var symbolInfo = info.Value;

                Symbol symbol = null;

                switch (layer.Layer.FeatureTable.GeometryType)
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

                if (key.Length == 0)
                {
                    renderer.DefaultSymbol = symbol;
                }
                else
                {
                    renderer.UniqueValues.Add(new UniqueValue(key, key, symbol, key));
                }
            }
            layer.Layer.Renderer = renderer;
        }

        private static void ApplyLabel(this MapLayerInfo layer)
        {
            LabelInfo label = layer.Label;

            layer.Layer.LabelDefinitions.Clear();
            layer.Layer.LabelDefinitions.Add(label.GetLabelDefinition());
            layer.Layer.LabelsEnabled = true;
        }

        public static LabelDefinition GetLabelDefinition(this LabelInfo label)
        {
            var exp = new ArcadeLabelExpression(label.GetExpression());
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
                MinScale = label.MinScale,
                TextLayout = (LabelTextLayout)label.Layout,
                RepeatStrategy = label.AllowRepeat ? LabelRepeatStrategy.Repeat : LabelRepeatStrategy.None,
                LabelOverlapStrategy = label.AllowOverlap ? LabelOverlapStrategy.Allow : LabelOverlapStrategy.Exclude,
                FeatureInteriorOverlapStrategy = label.AllowOverlap ? LabelOverlapStrategy.Allow : LabelOverlapStrategy.Exclude,
                FeatureBoundaryOverlapStrategy = label.AllowOverlap ? LabelOverlapStrategy.Allow : LabelOverlapStrategy.Exclude,
            };
            return labelDefinition;
        }

        private static string GetExpression(this LabelInfo label)
        {
            if (!string.IsNullOrEmpty(label.CustomLabelExpression))
            {
                return label.CustomLabelExpression;
            }
            string l = Parameters.LabelFieldName;
            string d = Parameters.DateFieldName;
            string c = Parameters.ClassFieldName;

            string newLine = $@"
if({label.NewLine})
{{
    exp=exp+'\n';
}}
else
{{
    exp=exp+'    ';
}}
";
            string exp = $@"
var exp='';
if({label.Info}&&$feature.{ l}!='')
{{
    exp=exp+$feature.{ l};
    {newLine}
}}
if({label.Date})
{{
if($feature.{ d}!=null)
{{
    exp=exp+Year($feature.{d})+'-'+(Month($feature.{d})+1)+'-'+Day($feature.{d});
    {newLine}
}}
}}
if({label.Class}&&$feature.{ c}!='')
{{
    exp=exp+$feature.{ c};
    {newLine}
}}
if({label.NewLine})
{{
    exp=Left(exp,Count(exp)-1);
}}
else
{{
    exp=Left(exp,Count(exp)-4);
}}
exp";
            return exp;
        }
    }
}