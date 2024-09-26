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
using System.Reflection.Emit;

namespace MapBoard.Util
{
    public static class StyleUtility
    {
        /// <summary>
        /// 应用符号系统和标注样式
        /// </summary>
        /// <param name="layer"></param>
        public static void ApplyStyle(this IMapLayerInfo layer)
        {
            layer.ApplyRenderer();
            layer.ApplyLabel();
        }

        /// <summary>
        /// 将MapBoard的<see cref="LabelInfo"/>转换为Esri的<see cref="LabelDefinition"/>
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public static LabelDefinition GetLabelDefinition(this LabelInfo label)
        {
            if (label.UseRawJson)
            {
                return LabelDefinition.FromJson(label.RawJson ?? "{}");
            }
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

        public static UniqueValueRendererInfo ToRendererInfo(this Renderer renderer, UniqueValueRendererInfo writeTo = null)
        {
            if (renderer is not UniqueValueRenderer && renderer is not SimpleRenderer)
            {
                throw new Exception("非唯一值渲染器或简单渲染器，无法转换");
            }

            UniqueValueRendererInfo r = writeTo ?? new UniqueValueRendererInfo();
            r.UseRawJson = false;
            if (renderer is SimpleRenderer s)
            {
                r.DefaultSymbol = s.Symbol.ToSymbolInfo();
            }
            else if (renderer is UniqueValueRenderer u)
            {
                r.DefaultSymbol = u.DefaultSymbol.ToSymbolInfo();
                r.KeyFieldName = string.Join('|', u.FieldNames);
                foreach (var uv in u.UniqueValues)
                {
                    r.Symbols.Add(string.Join('|', uv.Values.Select(p => p.ToString())), uv.Symbol.ToSymbolInfo());
                }
            }
            r.RawJson = renderer.ToJson();
            return r;

        }

        /// <summary>
        /// 将MapBoard的<see cref="SymbolInfo"/>转换为Esri的<see cref="Symbol"/>
        /// </summary>
        /// <param name="symbolInfo"></param>
        /// <param name="geometryType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 应用标注标签
        /// </summary>
        /// <param name="layer"></param>
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

        /// <summary>
        /// 应用符号系统
        /// </summary>
        /// <param name="layer"></param>
        private static void ApplyRenderer(this IMapLayerInfo layer)
        {
            if (layer.Renderer.UseRawJson)
            {
                layer.Layer.Renderer = Renderer.FromJson(layer.Renderer.RawJson ?? "{}");
                return;
            }
            switch (layer.Type)
            {
                case MapLayerInfo.Types.Shapefile:
                case MapLayerInfo.Types.Temp:
                case null:
                    {
                        UniqueValueRenderer renderer = new UniqueValueRenderer();
                        if (layer.Renderer.HasCustomSymbols)
                        {
                            //解析字段名
                            string[] names = layer.Renderer.KeyFieldName.Split('|', StringSplitOptions.RemoveEmptyEntries);
                            renderer.FieldNames.AddRange(names);

                            //对于每一个Key和Symbol
                            foreach (var info in layer.Renderer.Symbols)
                            {
                                try
                                {
                                    //解析key
                                    string[] keyStrings = info.Key.Split('|', StringSplitOptions.None);
                                    List<object> keys = new List<object>();

                                    //对每个key中对应的字段进行处理
                                    for (int i = 0; i < names.Length; i++)
                                    {
                                        var fieldType = layer.Fields.First(p => p.Name == names[i]).Type;
                                        //如果key的长度不到names的长度，那么就设为null
                                        object key = keyStrings.Length < i + 1 ? "" : keyStrings[i];
                                        //空字符串也为null
                                        if ((key as string) is "" or "（空）")
                                        {
                                            key = "";
                                        }
                                        //转换到正确的类型
                                        key = fieldType switch
                                        {
                                            FieldInfoType.Text or FieldInfoType.Time => key,
                                            FieldInfoType.Date => DateTime.Parse(key as string),
                                            FieldInfoType.Integer => int.Parse(key as string),
                                            FieldInfoType.Float => double.Parse(key as string),
                                            _ => throw new NotImplementedException(),
                                        };
                                        keys.Add(key);
                                    }


                                    renderer.UniqueValues.Add(new UniqueValue(info.Key, info.Key, info.Value.ToSymbol(layer.GeometryType), keys));

                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("转换唯一值渲染器的Key失败：" + ex.ToString());
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

        private static SymbolInfo ToSymbolInfo(this Symbol symbol, SymbolInfo writeTo = null)
        {
            SymbolInfo s = writeTo ?? new SymbolInfo();

            // 根据传入的符号类型进行不同的处理
            switch (symbol)
            {
                case SimpleMarkerSymbol marker:
                    s.PointStyle = (int)marker.Style;
                    s.FillColor = marker.Color;
                    s.Size = marker.Size;
                    if (marker.Outline != null)
                    {
                        s.LineStyle = (int)marker.Outline.Style;
                        s.LineColor = marker.Outline.Color;
                        s.OutlineWidth = marker.Outline.Width;
                    }
                    break;

                case SimpleLineSymbol line:
                    s.LineStyle = (int)line.Style;
                    s.LineColor = line.Color;
                    s.OutlineWidth = line.Width;
                    s.Arrow = (int)line.MarkerPlacement + 1;
                    break;

                case SimpleFillSymbol fill:
                    s.FillStyle = (int)fill.Style;
                    s.FillColor = fill.Color;
                    if (fill.Outline != null)
                    {
                        if (fill.Outline is SimpleLineSymbol sls)
                        {
                            s.LineStyle = (int)sls.Style;
                        }
                        s.LineColor = fill.Outline.Color;
                        s.OutlineWidth = fill.Outline.Width;
                    }
                    break;

                case MultilayerPolygonSymbol mp:
                    foreach (var sl in mp.SymbolLayers)
                    {
                        switch (sl)
                        {
                            case SolidFillSymbolLayer sf:
                                s.FillStyle = (int)SimpleFillSymbolStyle.Solid;
                                s.FillColor = sf.Color;
                                break;
                            case SolidStrokeSymbolLayer ss:
                                s.LineStyle = (int)SimpleLineSymbolStyle.Solid;
                                s.LineColor = ss.Color;
                                s.OutlineWidth = ss.Width;
                                break;
                        }
                    }
                    break;
            }

            return s;
        }
    }
}