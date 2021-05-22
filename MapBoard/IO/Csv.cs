﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
{
    public static class Csv
    {
        public static Task ExportAsync(IEnumerable<Feature> features, string path)
        {
            return Task.Run(() =>
            {
                Export(features, path);
            });
        }

        public static void Export(IEnumerable<Feature> features, string path)
        {
            StringBuilder sb = new StringBuilder();

            List<IEnumerable<MapPoint>> parts = new List<IEnumerable<MapPoint>>();
            Dictionary<object, int> featureIndexs = new Dictionary<object, int>();
            Dictionary<object, int> partIndexs = new Dictionary<object, int>();
            if (path != null)
            {
                int featureIndex = 0;
                int partIndex = 0;
                foreach (var feature in features)
                {
                    featureIndex++;
                    Geometry geometry = feature.Geometry;
                    dynamic part = null;
                    switch (feature.FeatureTable.GeometryType)
                    {
                        case GeometryType.Multipoint:
                            part = (geometry as Multipoint).Points;
                            featureIndexs.Add(part, featureIndex);
                            parts.Add(part);
                            break;

                        case GeometryType.Point:
                            part = new MapPoint[] { geometry as MapPoint };
                            featureIndexs.Add(part, featureIndex);
                            featureIndexs.Add(part, featureIndex);
                            break;

                        case GeometryType.Polygon:
                            partIndex = 0;
                            (geometry as Polygon).Parts.ForEach(p =>
                            {
                                partIndex++;
                                part = p.Points;
                                parts.Add(part);
                                featureIndexs.Add(part, featureIndex);
                                partIndexs.Add(part, partIndex);
                            });
                            break;

                        case GeometryType.Polyline:
                            partIndex = 0;
                            (geometry as Polyline).Parts.ForEach(p =>
                            {
                                partIndex++;
                                part = p.Points;
                                parts.Add(part);
                                featureIndexs.Add(part, featureIndex);
                                partIndexs.Add(part, partIndex);
                            });
                            break;
                    }
                }

                if (parts.Any())
                {
                    foreach (var part in parts)
                    {
                        Append("Feature" + featureIndexs[part], "Part" + (partIndexs.ContainsKey(part) ? partIndexs[part] : 1));
                        foreach (var point in part)
                        {
                            Append(point.X.ToString(), point.Y.ToString());
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(path, sb.ToString());
                }
                else
                {
                }
            }

            void Append(string col1, string col2)
            {
                sb.Append(col1).Append(',').Append(col2).AppendLine();
            }
        }

        public async static Task ImportAsync(string path)
        {
            string[] lines = File.ReadAllLines(path);
            List<List<MapPoint>> parts = new List<List<MapPoint>>();
            List<MapPoint> lastPoints = null;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    lastPoints = null;
                    continue;
                }
                string[] xy = line.Split(',');
                if (xy.Length != 2)
                {
                    throw new Exception("CSV格式不正确");
                }

                if (!double.TryParse(xy[0], out double x) || !double.TryParse(xy[1], out double y))
                {
                    continue;
                }

                if (lastPoints == null)
                {
                    lastPoints = new List<MapPoint>();
                    parts.Add(lastPoints);
                }

                lastPoints.Add(new MapPoint(x, y));
            }

            List<Feature> features = new List<Feature>();
            var style = MapLayerCollection.Instance.Selected;

            foreach (var part in parts)
            {
                Feature feature = style.Table.CreateFeature();
                features.Add(feature);
                switch (style.Table.GeometryType)
                {
                    case GeometryType.Multipoint:
                        feature.Geometry = new Multipoint(part);
                        break;

                    case GeometryType.Point:
                        if (part.Count != 1)
                        {
                            throw new Exception("若要导入为点，每一部分必须只有一个坐标");
                        }
                        feature.Geometry = part[0];

                        break;

                    case GeometryType.Polygon:
                        feature.Geometry = new Polygon(part);
                        break;

                    case GeometryType.Polyline:
                        feature.Geometry = new Polyline(part);
                        break;
                }
            }
            await style.Table.AddFeaturesAsync(features);
        }
    }
}