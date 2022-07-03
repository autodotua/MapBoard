using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Data;
using CsvHelper;
using System.Globalization;
using FzLib;
using System.Dynamic;

namespace MapBoard.IO
{
    public static class Csv
    {
        public static Task ExportAsync(string path, Feature[] features)
        {
            return Task.Run(() =>
            {
                Export(path, features);
            });
        }

        public static void Export(string path, Feature[] features)
        {
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
                            parts.Add(part);
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

                using var writer = new StreamWriter(path,false,new UTF8Encoding(true));
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                List<dynamic> records=new List<dynamic>();
                if (parts.Any())
                {
                    foreach (var part in parts)
                    {
                        var featureID = featureIndexs[part];
                        var partID = partIndexs.ContainsKey(part) ? partIndexs[part] : 1;

                        int id = 0;
                        foreach (var point in part)
                        {
                            dynamic record = new ExpandoObject();
                          var dic=  record as IDictionary<string, object>;
                            dic.Add("FeatureIndex", featureID);
                            dic.Add("PartIndex", partID);
                            dic.Add("PointIndex", ++id);
                            record.X = point.X;
                            record.Y = point.Y;
                            foreach (var attr in features[featureID-1].Attributes)
                            {
                                dic.Add(attr.Key,attr.Value);
                            }
                            records.Add(record);
                        }
                    }
                    csv.WriteRecords(records);
                    csv.Flush();
                }
                else
                {
                }
            }
        }

        public static async Task<IReadOnlyList<Feature>> ImportAsync(string path, IEditableLayerInfo layer)
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
                    throw new FormatException("CSV格式不正确");
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

            foreach (var part in parts)
            {
                Feature feature = layer.CreateFeature();
                features.Add(feature);
                switch (layer.GeometryType)
                {
                    case GeometryType.Multipoint:
                        feature.Geometry = new Multipoint(part);
                        break;

                    case GeometryType.Point:
                        if (part.Count != 1)
                        {
                            throw new FormatException("若要导入为点，每一部分必须只有一个坐标");
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
            await layer.AddFeaturesAsync(features, FeaturesChangedSource.Import);
            return features.AsReadOnly();
        }

        public static Task<DataTable> ImportToDataTableAsync(string path)
        {
            return Task.Run(() =>
            {
                using var reader = new StreamReader(path);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                using var dr = new CsvDataReader(csv);
                using var dt = new DataTable();
                dt.Load(dr);
                return dt;
            });
        }
    }
}