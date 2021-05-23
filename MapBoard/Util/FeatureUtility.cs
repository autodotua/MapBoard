using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Main.Util
{
    public static class FeatureUtility
    {
        public static async Task<Feature> UnionAsync(LayerInfo layer, Feature[] features)
        {
            Geometry geometry = GeometryEngine.Union(features.Select(p => p.Geometry));
            var firstFeature = features.First();
            var newFeature = layer.Table.CreateFeature(firstFeature.Attributes, geometry);
            await layer.Table.DeleteFeaturesAsync(features);
            await layer.Table.AddFeatureAsync(newFeature);
            FeaturesGeometryChanged?.Invoke(null,
                new FeaturesGeometryChangedEventArgs(layer, new[] { newFeature }, features, null));
            return newFeature;
        }

        public static async Task<IReadOnlyList<Feature>> SeparateAsync(LayerInfo layer, Feature[] features)
        {
            List<Feature> deleted = new List<Feature>();
            List<Feature> added = new List<Feature>();
            foreach (var feature in features)
            {
                Debug.Assert(feature.Geometry is Multipart);
                var m = feature.Geometry as Multipart;
                if (m.Parts.Count <= 1)
                {
                    continue;
                }
                foreach (var part in m.Parts)
                {
                    Geometry g = m is Polyline ? new Polyline(part) : new Polygon(part);
                    var newFeature = layer.Table.CreateFeature(feature.Attributes, g);
                    added.Add(newFeature);
                }
                deleted.Add(feature);
            }
            if (added.Count > 0)
            {
                await layer.Table.AddFeaturesAsync(added);
                await layer.Table.DeleteFeaturesAsync(deleted);
                FeaturesGeometryChanged?.Invoke(null,
       new FeaturesGeometryChangedEventArgs(layer, added, deleted, null));
            }
            return added.AsReadOnly();
        }

        public static async Task<Feature> LinkAsync(LayerInfo layer, Feature[] features, bool headToHead, bool reverse)
        {
            List<MapPoint> points = null;
            if (features.Length <= 1)
            {
                throw new ArgumentException("要素数量小于2");
            }
            if (features.Length == 2)
            {
                List<MapPoint> points1 = GetPoints(features[0]);
                List<MapPoint> points2 = GetPoints(features[1]);
                if (headToHead && !reverse)
                {
                    points1.Reverse();
                    points1.AddRange(points2);
                }
                else if (headToHead && reverse)
                {
                    points2.Reverse();
                    points1.AddRange(points2);
                }
                else if (!headToHead && !reverse)
                {
                    points1.AddRange(points2);
                }
                else if (!headToHead && reverse)
                {
                    points1.InsertRange(0, points2);
                }

                points = points1;
            }
            else
            {
                IEnumerable<List<MapPoint>> pointsGroup = features.Select(p => GetPoints(p));
                if (reverse)
                {
                    pointsGroup = pointsGroup.Reverse();
                }
                points = new List<MapPoint>();
                foreach (var part in pointsGroup)
                {
                    points.AddRange(part);
                }
            }
            var newFeature = layer.Table.CreateFeature(features[0].Attributes, new Polyline(points));

            await layer.Table.AddFeatureAsync(newFeature);

            await layer.Table.DeleteFeaturesAsync(features);
            FeaturesGeometryChanged?.Invoke(null,
    new FeaturesGeometryChangedEventArgs(layer, new[] { newFeature }, features, null));
            return newFeature;
        }

        public static async Task<IReadOnlyList<Feature>> ReverseAsync(LayerInfo layer, Feature[] features)
        {
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in features.ToList())
            {
                List<MapPoint> points = GetPoints(feature);
                points.Reverse();
                Geometry newGeo = null;
                if (layer.Table.GeometryType == GeometryType.Polygon)
                {
                    newGeo = new Polygon(points.ToList());
                }
                else if (layer.Table.GeometryType == GeometryType.Polyline)
                {
                    newGeo = new Polyline(points.ToList());
                }
                await layer.Table.DeleteFeatureAsync(feature);
                var newFeature = layer.Table.CreateFeature(feature.Attributes, newGeo);
                await layer.Table.AddFeatureAsync(newFeature);
                newFeatures.Add(newFeature);
            }
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, newFeatures, features, null));
            return newFeatures.AsReadOnly();
        }

        public static async Task DensifyAsync(LayerInfo layer, Feature[] features, double max)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();
            foreach (var feature in features.ToList())
            {
                var newGeo = GeometryEngine.DensifyGeodetic(feature.Geometry, max, LinearUnits.Meters);
                oldGeometries.Add(feature, feature.Geometry);
                feature.Geometry = newGeo;
            }
            await layer.Table.UpdateFeaturesAsync(oldGeometries.Keys);
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, null, null, oldGeometries));
        }

        public async static Task VerticalDistanceSimplifyAsync(LayerInfo layer, Feature[] features, double max)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();
            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);
                await SimplyBaseAsync(layer, feature, part =>
                {
                    HashSet<MapPoint> deletedPoints = new HashSet<MapPoint>();
                    for (int i = 2; i < part.PointCount; i += 2)
                    {
                        var dist = GetVerticleDistance(part.Points[i - 2], part.Points[i], part.Points[i - 1]);
                        if (dist < max)
                        {
                            deletedPoints.Add(part.Points[i - 1]);
                        }
                    }
                    return part.Points.Except(deletedPoints);
                });
            }
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, null, null, oldGeometries));
        }

        public async static Task GeneralizeSimplifyAsync(LayerInfo layer, Feature[] features, double max)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();

            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);

                Geometry geometry = feature.Geometry;
                geometry = GeometryEngine.Project(geometry, SpatialReferences.WebMercator);
                geometry = GeometryEngine.Generalize(geometry, max, false);
                geometry = GeometryEngine.Project(geometry, SpatialReferences.Wgs84);
                feature.Geometry = geometry;
                await layer.Table.UpdateFeatureAsync(feature);
            }
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, null, null, oldGeometries));
        }

        private static async Task SimplyBaseAsync(LayerInfo layer, Feature feature,
            Func<ReadOnlyPart, IEnumerable<MapPoint>> func)
        {
            Debug.Assert(layer.Table.GeometryType == GeometryType.Polygon || layer.Table.GeometryType == GeometryType.Polyline); ;

            IReadOnlyList<ReadOnlyPart> parts = null;
            if (layer.Table.GeometryType == GeometryType.Polygon)
            {
                parts = (feature.Geometry as Polygon).Parts;
            }
            else if (layer.Table.GeometryType == GeometryType.Polyline)
            {
                parts = (feature.Geometry as Polyline).Parts;
            }

            List<IEnumerable<MapPoint>> newParts = new List<IEnumerable<MapPoint>>();
            foreach (var part in parts)
            {
                newParts.Add(func(part));
            }
            if (layer.Table.GeometryType == GeometryType.Polygon)
            {
                feature.Geometry = new Polygon(newParts, feature.Geometry.SpatialReference);
            }
            else if (layer.Table.GeometryType == GeometryType.Polyline)
            {
                feature.Geometry = new Polyline(newParts, feature.Geometry.SpatialReference);
            }
            await layer.Table.UpdateFeatureAsync(feature);
        }

        /// <summary>
        /// 获取一个点到两点连线的垂距
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <param name="pc">中心点</param>
        /// <returns></returns>
        private static double GetVerticleDistance(MapPoint p1, MapPoint p2, MapPoint pc)
        {
            var p1P2Line = new Polyline(new MapPoint[] { p1, p2 });
            return GetVerticleDistance(p1P2Line, pc);
        }

        private static double GetVerticleDistance(Polyline line, MapPoint pc)
        {
            var nearestPoint = GeometryEngine.NearestCoordinate(line, pc);
            var dist = GeometryEngine.DistanceGeodetic(pc, nearestPoint.Coordinate, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.NormalSection);
            return dist.Distance;
        }

        public static async Task DouglasPeuckerSimplifyAsync(LayerInfo layer, Feature[] features, double max)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();

            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);

                await SimplyBaseAsync(layer, feature, part =>
                {
                    var ps = part.Points;
                    IList<MapPoint> Recurse(int from, int to)
                    {
                        if (to - 1 == from)
                        {
                            return ps.Skip(from).Take(2).ToList();
                        }
                        var line = new Polyline(new MapPoint[] { ps[from], ps[to] });
                        double maxDist = 0;
                        int maxDistPointIndex = -1;
                        for (int i = from + 1; i < to; i++)
                        {
                            double dist = GetVerticleDistance(line, ps[i]);
                            if (dist > maxDist)
                            {
                                maxDist = dist;
                                maxDistPointIndex = i;
                            }
                        }
                        if (maxDist < max)
                        {
                            return new MapPoint[] { ps[from], ps[to] };
                        }
                        return Recurse(from, maxDistPointIndex)
                        .Concat(Recurse(maxDistPointIndex, to).Skip(1))//中间那个点大家都会取到，所以要去掉
                        .ToList();
                    }
                    var result = Recurse(0, part.PointCount - 1);
                    return result;
                });
            }
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, null, null, oldGeometries));
        }

        public static async Task IntervalTakePointsSimplifyAsync(LayerInfo layer, Feature[] features, double interval)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();
            if (interval < 2)
            {
                throw new Exception("间隔不应小于2");
            }
            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);

                await SimplyBaseAsync(layer, feature, part =>
                {
                    List<MapPoint> points = new List<MapPoint>();
                    for (int i = 0; i < part.PointCount; i++)
                    {
                        if (i % interval == 0)
                        {
                            points.Add(part.Points[i]);
                        }
                    }
                    if ((part.PointCount - 1) % interval != 0)
                    {
                        points.Add(part.Points[part.PointCount - 1]);
                    }
                    return points;
                });
            }
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, null, null, oldGeometries));
        }

        public static async Task<IReadOnlyList<Feature>> CreateCopyAsync(LayerInfo layer, Feature[] features)
        {
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in features)
            {
                Feature newFeature = layer.Table.CreateFeature(feature.Attributes, feature.Geometry);
                newFeatures.Add(newFeature);
            }
            await layer.Table.AddFeaturesAsync(newFeatures);
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, newFeatures, null, null));
            return newFeatures.AsReadOnly();
        }

        public static async Task DeleteAsync(LayerInfo layer, Feature[] features)
        {
            await layer.Table.DeleteFeaturesAsync(features);
            FeaturesGeometryChanged?.Invoke(null,
new FeaturesGeometryChangedEventArgs(layer, null, features, null));
        }

        public static async Task<IReadOnlyList<Feature>> CutAsync(LayerInfo layer, Feature[] features, Polyline clipLine)
        {
            List<Feature> added = new List<Feature>();
            foreach (var feature in features)
            {
                var newGeos = GeometryEngine.Cut(feature.Geometry,
                    GeometryEngine.Project(clipLine, SpatialReferences.Wgs84) as Polyline);
                int partsCount = 0;
                foreach (var newGeo in newGeos)
                {
                    foreach (var part in (newGeo as Multipart).Parts)
                    {
                        partsCount++;
                        Feature newFeature = layer.Table.CreateFeature(
                            feature.Attributes, newGeo is Polyline ? new Polyline(part) : new Polygon(part));
                        added.Add(newFeature);
                    }
                }
            }
            await layer.Table.AddFeaturesAsync(added);
            await layer.Table.DeleteFeaturesAsync(features);
            FeaturesGeometryChanged?.Invoke(null,
    new FeaturesGeometryChangedEventArgs(layer, added, features, null));
            return added;
        }

        private static List<MapPoint> GetPoints(Feature feature)
        {
            IReadOnlyList<ReadOnlyPart> parts = null;
            if (feature.Geometry is Polyline line)
            {
                parts = line.Parts;
            }
            else if (feature.Geometry is Polygon polygon)
            {
                parts = polygon.Parts;
            }
            if (parts.Count > 1)
            {
                throw new Exception("不支持操作拥有多个部分的要素");
            }
            List<MapPoint> points = new List<MapPoint>();
            foreach (var part in parts)
            {
                points.AddRange(part.Points);
            }
            return points;
        }

        public static async Task<IReadOnlyList<Feature>> CopyOrMoveAsync(LayerInfo layerFrom, LayerInfo layerTo, Feature[] features, bool copy)
        {
            layerTo.LayerVisible = true;
            ShapefileFeatureTable targetTable = layerTo.Table;
            var newFeatures = new List<Feature>();
            var fields = targetTable.Fields.Select(p => p.Name).ToHashSet();
            foreach (var feature in await layerFrom.GetAllFeaturesAsync())
            {
                Dictionary<string, object> attributes = new Dictionary<string, object>();
                foreach (var attr in feature.Attributes)
                {
                    if (fields.Contains(attr.Key))
                    {
                        attributes.Add(attr.Key, attr.Value);
                    }
                }
                newFeatures.Add(targetTable.CreateFeature(attributes, feature.Geometry));
            }
            await targetTable.AddFeaturesAsync(newFeatures);
            if (!copy)
            {
                await DeleteAsync(layerFrom, features);
            }
            layerTo.NotifyFeatureChanged();
            return newFeatures.AsReadOnly();
        }

        public static event EventHandler<FeaturesGeometryChangedEventArgs> FeaturesGeometryChanged;

        public static async Task CopyAttributesAsync(LayerInfo layer, FieldInfo fieldSource, FieldInfo fieldTarget, string dateFormat)
        {
            var features = await layer.GetAllFeaturesAsync();
            foreach (var feature in features)
            {
                object value = feature.Attributes[fieldSource.Name];
                if (value is DateTimeOffset dto)
                {
                    value = dto.UtcDateTime;
                }
                if (fieldTarget.Type == fieldSource.Type)
                {
                    feature.SetAttributeValue(fieldTarget.Name, feature.GetAttributeValue(fieldSource.Name));
                }
                else
                {
                    object result = null;
                    try
                    {
                        switch (fieldTarget.Type)
                        {
                            case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Float:
                                result = Convert.ToInt32(value);
                                break;

                            case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Text:
                                result = int.Parse(value as string);
                                break;

                            case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Integer:
                                result = Convert.ToDouble(value);
                                break;

                            case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Text:
                                result = double.Parse(value as string);
                                break;

                            case FieldInfoType.Date when fieldSource.Type == FieldInfoType.Text:
                                result = DateTime.ParseExact(value as string, dateFormat, CultureInfo.CurrentCulture);
                                break;

                            case FieldInfoType.Text when fieldSource.Type == FieldInfoType.Date:
                                result = ((DateTime)value).Date.ToString(dateFormat);
                                break;

                            case FieldInfoType.Text:
                                result = value.ToString();
                                break;
                        }
                    }
                    catch { }
                    feature.SetAttributeValue(fieldTarget.Name, result);
                }
                await layer.Table.UpdateFeatureAsync(feature);
            }
        }
    }

    public class FeaturesGeometryChangedEventArgs : EventArgs
    {
        public IReadOnlyList<Feature> DeletedFeatures { get; }
        public IReadOnlyList<Feature> AddedFeatures { get; }
        public IReadOnlyDictionary<Feature, Geometry> ChangedFeatures { get; }
        public LayerInfo Layer { get; }

        public FeaturesGeometryChangedEventArgs(LayerInfo layer,
            IEnumerable<Feature> addedFeatures,
            IEnumerable<Feature> deletedFeatures,
            IDictionary<Feature, Geometry> changedFeatures)
        {
            if (deletedFeatures != null)
            {
                DeletedFeatures = new List<Feature>(deletedFeatures).AsReadOnly();
            }
            if (addedFeatures != null)
            {
                AddedFeatures = new List<Feature>(addedFeatures).AsReadOnly();
            }
            if (changedFeatures != null)
            {
                ChangedFeatures = new ReadOnlyDictionary<Feature, Geometry>(changedFeatures);
            }
            Layer = layer;
        }
    }
}