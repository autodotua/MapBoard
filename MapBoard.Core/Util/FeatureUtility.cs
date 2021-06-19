using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static MapBoard.Mapping.Model.FeaturesChangedSource;
using MapBoard.Mapping.Model;

namespace MapBoard.Util
{
    public static class FeatureUtility
    {
        public static async Task<Feature> UnionAsync(MapLayerInfo layer, Feature[] features)
        {
            Geometry geometry = GeometryEngine.Union(features.Select(p => p.Geometry));
            var firstFeature = features.First();
            var newFeature = layer.CreateFeature(firstFeature.Attributes, geometry);
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeatureAsync(newFeature, FeatureOperation);
            return newFeature;
        }

        public static async Task<IReadOnlyList<Feature>> SeparateAsync(MapLayerInfo layer, Feature[] features)
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
                    Geometry g = m is Polyline ? (Geometry)new Polyline(part) : (Geometry)new Polygon(part);
                    var newFeature = layer.CreateFeature(feature.Attributes, g);
                    added.Add(newFeature);
                }
                deleted.Add(feature);
            }
            if (added.Count > 0)
            {
                await layer.DeleteFeaturesAsync(deleted, FeatureOperation);
                await layer.AddFeaturesAsync(added, FeatureOperation);
            }
            return added.AsReadOnly();
        }

        public static async Task<Feature> AutoLinkAsync(MapLayerInfo layer, Feature[] features)
        {
            List<MapPoint> points = new List<MapPoint>();
            if (features.Length <= 1)
            {
                throw new ArgumentException("要素数量小于2");
            }

            if (features.Any(p => p.Geometry.GeometryType != GeometryType.Polyline))
            {
                throw new ArgumentException("不是折线类型");
            }

            List<ReadOnlyPart> parts = new List<ReadOnlyPart>();
            foreach (var f in features)
            {
                parts.AddRange((f.Geometry as Polyline).Parts);
            }
            var line1 = parts[0];
            var line2 = parts[1];

            double min = Math.Min(
                GeometryUtility.GetDistance(line1.StartPoint, line2.StartPoint),
                GeometryUtility.GetDistance(line1.StartPoint, line2.EndPoint));
            //首先假设，最近点是起点，那么连接后的线起点就是line1的终点
            int start = 1;//0代表起点，1代表终点

            //如果发现终点到line2起点或终点的距离更小，那么最近点就是line1的终点
            if (Math.Min(
                GeometryUtility.GetDistance(line1.EndPoint, line2.StartPoint),
                GeometryUtility.GetDistance(line1.EndPoint, line2.EndPoint)) < min)
            {
                start = 0;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                ReadOnlyPart part1 = parts[i];
                ReadOnlyPart part2 = null;
                if (i < parts.Count - 1)
                {
                    part2 = parts[i + 1];
                }
                if (start == 0)//顺序不变
                {
                    points.AddRange(part1.Points);
                }
                else//逆序
                {
                    points.AddRange(part1.Points.Reverse());
                }
                if (i < parts.Count - 1)
                {
                    //part2需要连接的点
                    MapPoint p = start == 0 ? part1.EndPoint : part1.StartPoint;
                    //寻找part2中离p最近的点

                    start = GeometryUtility.GetDistance(p, part2.StartPoint)
                        < GeometryUtility.GetDistance(p, part2.EndPoint) ?
                        0 : 1;
                }
            }

            var newFeature = layer.CreateFeature(features[0].Attributes, new Polyline(points));

            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeatureAsync(newFeature, FeatureOperation);

            return newFeature;
        }

        public static async Task<Feature> LinkAsync(MapLayerInfo layer, Feature[] features, bool headToHead, bool reverse)
        {
            List<MapPoint> points = null;
            if (features.Length <= 1)
            {
                throw new ArgumentException("要素数量小于2");
            }

            if (features.Any(p => p.Geometry.GeometryType != GeometryType.Polyline))
            {
                throw new ArgumentException("不是折线类型");
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
            var newFeature = layer.CreateFeature(features[0].Attributes, new Polyline(points));

            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeatureAsync(newFeature, FeatureOperation);

            return newFeature;
        }

        public static async Task<IReadOnlyList<Feature>> ReverseAsync(MapLayerInfo layer, Feature[] features)
        {
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in features.ToList())
            {
                List<MapPoint> points = GetPoints(feature);
                points.Reverse();
                Geometry newGeo = null;
                if (layer.GeometryType == GeometryType.Polygon)
                {
                    newGeo = new Polygon(points.ToList());
                }
                else if (layer.GeometryType == GeometryType.Polyline)
                {
                    newGeo = new Polyline(points.ToList());
                }
                var newFeature = layer.CreateFeature(feature.Attributes, newGeo);
                newFeatures.Add(newFeature);
            }
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeaturesAsync(newFeatures, FeatureOperation);
            return newFeatures.AsReadOnly();
        }

        public static async Task DensifyAsync(MapLayerInfo layer, Feature[] features, double max)
        {
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();
            foreach (var feature in features.ToList())
            {
                var newGeo = GeometryEngine.DensifyGeodetic(feature.Geometry, max, LinearUnits.Meters);
                newFeatures.Add(new UpdatedFeature(feature, feature.Geometry, feature.Attributes));
                feature.Geometry = newGeo;
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
        }

        public async static Task VerticalDistanceSimplifyAsync(MapLayerInfo layer, Feature[] features, double max)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);
                newFeatures.Add(SimplyBase(layer, feature, part =>
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
               }));
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
        }

        public async static Task GeneralizeSimplifyAsync(MapLayerInfo layer, Feature[] features, double max)
        {
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            foreach (var feature in features)
            {
                Geometry geometry = feature.Geometry;
                geometry = GeometryEngine.Project(geometry, SpatialReferences.WebMercator);
                geometry = GeometryEngine.Generalize(geometry, max, false);
                geometry = GeometryEngine.Project(geometry, SpatialReferences.Wgs84);
                newFeatures.Add(new UpdatedFeature(feature, geometry, feature.Attributes));
                feature.Geometry = geometry;
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
        }

        private static UpdatedFeature SimplyBase(MapLayerInfo layer, Feature feature,
            Func<ReadOnlyPart, IEnumerable<MapPoint>> func)
        {
            Debug.Assert(layer.GeometryType == GeometryType.Polygon || layer.GeometryType == GeometryType.Polyline); ;

            IReadOnlyList<ReadOnlyPart> parts = null;
            if (layer.GeometryType == GeometryType.Polygon)
            {
                parts = (feature.Geometry as Polygon).Parts;
            }
            else if (layer.GeometryType == GeometryType.Polyline)
            {
                parts = (feature.Geometry as Polyline).Parts;
            }
            List<IEnumerable<MapPoint>> newParts = new List<IEnumerable<MapPoint>>();
            foreach (var part in parts)
            {
                newParts.Add(func(part));
            }
            UpdatedFeature uf = new UpdatedFeature(feature);
            if (layer.GeometryType == GeometryType.Polygon)
            {
                feature.Geometry = new Polygon(newParts, feature.Geometry.SpatialReference);
            }
            else if (layer.GeometryType == GeometryType.Polyline)
            {
                feature.Geometry = new Polyline(newParts, feature.Geometry.SpatialReference);
            }
            return uf;
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

        public static async Task DouglasPeuckerSimplifyAsync(MapLayerInfo layer, Feature[] features, double max)
        {
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();

            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);

                newFeatures.Add(SimplyBase(layer, feature, part =>
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
                }));
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
        }

        public static async Task IntervalTakePointsSimplifyAsync(MapLayerInfo layer, Feature[] features, double interval)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            if (interval < 2)
            {
                throw new Exception("间隔不应小于2");
            }
            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);

                newFeatures.Add(SimplyBase(layer, feature, part =>
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
               }));
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
        }

        public static async Task<IReadOnlyList<Feature>> CreateCopyAsync(MapLayerInfo layer, Feature[] features)
        {
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in features)
            {
                Feature newFeature = layer.CreateFeature(feature.Attributes, feature.Geometry);
                newFeatures.Add(newFeature);
            }
            await layer.AddFeaturesAsync(newFeatures, FeatureOperation);
            return newFeatures.AsReadOnly();
        }

        public static async Task DeleteAsync(MapLayerInfo layer, Feature[] features)
        {
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
        }

        public static async Task<IReadOnlyList<Feature>> CutAsync(MapLayerInfo layer, Feature[] features, Polyline clipLine)
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
                        Feature newFeature = layer.CreateFeature(
                            feature.Attributes, newGeo is Polyline ? (Geometry)new Polyline(part) : (Geometry)new Polygon(part));
                        added.Add(newFeature);
                    }
                }
            }
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeaturesAsync(added, FeatureOperation);
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

        public static async Task<IReadOnlyList<Feature>> CopyOrMoveAsync(MapLayerInfo layerFrom, MapLayerInfo layerTo, Feature[] features, bool copy)
        {
            var newFeatures = new List<Feature>();
            var fields = layerTo.Fields.Select(p => p.Name).ToHashSet();
            foreach (var feature in features)
            {
                Dictionary<string, object> attributes = new Dictionary<string, object>();
                foreach (var attr in feature.Attributes)
                {
                    if (fields.Contains(attr.Key))
                    {
                        attributes.Add(attr.Key, attr.Value);
                    }
                }
                newFeatures.Add(layerTo.CreateFeature(attributes, feature.Geometry));
            }
            await layerTo.AddFeaturesAsync(newFeatures, FeatureOperation);
            if (!copy)
            {
                await DeleteAsync(layerFrom, features);
            }
            layerFrom.LayerVisible = false;
            layerTo.LayerVisible = true;
            return newFeatures.AsReadOnly();
        }

        public static async Task CopyAttributesAsync(MapLayerInfo layer, FieldInfo fieldSource, FieldInfo fieldTarget, string dateFormat)
        {
            var features = await layer.GetAllFeaturesAsync();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            foreach (var feature in features)
            {
                newFeatures.Add(new UpdatedFeature(feature, feature.Geometry, new Dictionary<string, object>(feature.Attributes)));
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
                            //小数转整数
                            case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Float:
                                result = Convert.ToInt32(value);
                                break;
                            //文本转整数
                            case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Text:
                                result = int.Parse(value as string);
                                break;
                            //整数转小数
                            case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Integer:
                                result = Convert.ToDouble(value);
                                break;
                            //文本转小数
                            case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Text:
                                result = double.Parse(value as string);
                                break;
                            //文本转日期
                            case FieldInfoType.Date when fieldSource.Type == FieldInfoType.Text:
                                result = DateTime.ParseExact(value as string, dateFormat, CultureInfo.CurrentCulture);
                                break;
                            //文本转时间
                            case FieldInfoType.Time when fieldSource.Type == FieldInfoType.Text:
                                result = value;
                                break;
                            //日期转文本
                            case FieldInfoType.Text when fieldSource.Type == FieldInfoType.Date:
                                result = ((DateTime)value).Date.ToString(dateFormat);
                                break;
                            //任意转文本
                            case FieldInfoType.Text:
                                result = value.ToString();
                                break;

                            default:
                                throw new Exception();
                        }
                    }
                    catch { }
                    feature.SetAttributeValue(fieldTarget.Name, result);
                }
            }
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
        }
    }
}