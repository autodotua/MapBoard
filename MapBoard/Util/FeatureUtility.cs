using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.UI.Dialog;
using MapBoard.Common.Dialog;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Util
{
    public static class FeatureUtility
    {
        public static async Task Union(LayerInfo layer, IEnumerable<Feature> features)
        {
            Geometry geometry = GeometryEngine.Union(features.Select(p => p.Geometry));
            var firstFeature = features.First();
            firstFeature.Geometry = geometry;
            await layer.Table.UpdateFeatureAsync(firstFeature);
            await layer.Table.DeleteFeaturesAsync(ArcMapView.Instance.Selection.SelectedFeatures.Where(p => p != firstFeature));
            ArcMapView.Instance.Selection.ClearSelection();
        }

        public static async Task Link(LayerInfo layer, IList<Feature> features, bool headToHead, bool reverse)
        {
            List<MapPoint> points = null;
            if (features.Count <= 1)
            {
                throw new ArgumentException("要素数量小于2");
            }
            if (features.Count == 2)
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
            features[0].Geometry = new Polyline(points);

            await layer.Table.UpdateFeatureAsync(features[0]);

            await layer.Table.DeleteFeaturesAsync(features.Where(p => p != features[0]));
            ArcMapView.Instance.Selection.ClearSelection();
        }

        public static async Task Reverse(LayerInfo layer, IEnumerable<Feature> features)
        {
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
                await layer.Table.AddFeatureAsync(layer.Table.CreateFeature(feature.Attributes, newGeo));
                ArcMapView.Instance.Selection.ClearSelection();
            }
        }

        public static async Task Densify(LayerInfo layer, IEnumerable<Feature> features, double max)
        {
            foreach (var feature in features.ToList())
            {
                feature.Geometry = GeometryEngine.DensifyGeodetic(feature.Geometry, max, LinearUnits.Meters);
                await layer.Table.UpdateFeatureAsync(feature);
            }
            ArcMapView.Instance.Selection.ClearSelection();
        }

        public async static Task VerticalDistanceSimplify(LayerInfo layer, IEnumerable<Feature> features, double max)
        {
            foreach (var feature in features)
            {
                await SimplyBase(layer, feature, part =>
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
        }

        public async static Task GeneralizeSimplify(LayerInfo layer, IEnumerable<Feature> features, double max)
        {
            foreach (var feature in features)
            {
                Geometry geometry = feature.Geometry;
                geometry = GeometryEngine.Project(geometry, SpatialReferences.WebMercator);
               geometry= GeometryEngine.Generalize(geometry, max, false);
                geometry = GeometryEngine.Project(geometry, SpatialReferences.Wgs84);
                feature.Geometry = geometry;
                await layer.Table.UpdateFeatureAsync(feature);
            }
        }

        private static async Task SimplyBase(LayerInfo layer, Feature feature,
            Func<ReadOnlyPart, IEnumerable<MapPoint>> func)
        {
            Debug.Assert(layer.Type == GeometryType.Polygon || layer.Type == GeometryType.Polyline); ;

            IReadOnlyList<ReadOnlyPart> parts = null;
            if (layer.Type == GeometryType.Polygon)
            {
                parts = (feature.Geometry as Polygon).Parts;
            }
            else if (layer.Type == GeometryType.Polyline)
            {
                parts = (feature.Geometry as Polyline).Parts;
            }

            List<IEnumerable<MapPoint>> newParts = new List<IEnumerable<MapPoint>>();
            foreach (var part in parts)
            {
                newParts.Add(func(part));
            }
            if (layer.Type == GeometryType.Polygon)
            {
                feature.Geometry = new Polygon(newParts, feature.Geometry.SpatialReference);
            }
            else if (layer.Type == GeometryType.Polyline)
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

        public static async Task DouglasPeuckerSimplify(LayerInfo layer, IEnumerable<Feature> features, double max)
        {
            foreach (var feature in features)
            {
                await SimplyBase(layer, feature, part =>
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
        }

        public static async Task IntervalTakePointsSimplify(LayerInfo layer, IEnumerable<Feature> features, double interval)
        {
            foreach (var feature in features)
            {
                if (interval < 2)
                {
                    throw new Exception("间隔不应小于2");
                }
                await SimplyBase(layer, feature, part =>
                {
                    List<MapPoint> points = new List<MapPoint>();
                    for (int i = 0; i < part.PointCount; i++)
                    {
                        if (i % interval == 0)
                        {
                            points.Add(part.Points[i]);
                        }
                    }
                    if (part.PointCount % interval != 0)
                    {
                        points.Add(part.Points[part.PointCount - 1]);
                    }
                    return points;
                });
            }
        }

        public static async Task IntervalTakePointsSimplify(LayerInfo layer)
        {
            Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];
            int? num = await CommonDialog.ShowIntInputDialogAsync("请输入每几个点保留一个点");

            if (num.HasValue)
            {
                int eachPoint = num.Value;
                if (eachPoint < 2)
                {
                    await CommonDialog.ShowErrorDialogAsync("输入的值不可小于2！");
                    return;
                }

                if (layer.Type == GeometryType.Polygon)
                {
                    Polygon polygon = feature.Geometry as Polygon;
                    List<List<MapPoint>> newParts = new List<List<MapPoint>>();
                    foreach (var part in polygon.Parts)
                    {
                        List<MapPoint> points = new List<MapPoint>();
                        for (int i = 0; i < part.PointCount; i++)
                        {
                            if (i % eachPoint == 0)
                            {
                                points.Add(part.Points[i]);
                            }
                        }
                        newParts.Add(points);
                    }
                    Polygon newPolygon = new Polygon(newParts);
                    feature.Geometry = newPolygon;
                }
                else
                {
                    Polyline polygon = feature.Geometry as Polyline;
                    List<List<MapPoint>> newParts = new List<List<MapPoint>>();
                    foreach (var part in polygon.Parts)
                    {
                        List<MapPoint> points = new List<MapPoint>();
                        for (int i = 0; i < part.PointCount; i++)
                        {
                            if (i % eachPoint == 0)
                            {
                                points.Add(part.Points[i]);
                            }
                        }
                        newParts.Add(points);
                    }
                    Polyline newPolygon = new Polyline(newParts);
                    feature.Geometry = newPolygon;
                }
                await layer.Table.UpdateFeatureAsync(feature);
            }
        }

        public static async Task CreateCopy(LayerInfo layer, IEnumerable<Feature> features)
        {
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in features)
            {
                Feature newFeature = layer.Table.CreateFeature(feature.Attributes, feature.Geometry);
                newFeatures.Add(newFeature);
            }
            await layer.Table.AddFeaturesAsync(newFeatures);
            ArcMapView.Instance.Selection.ClearSelection();
            ArcMapView.Instance.Selection.Select(newFeatures);
            layer.UpdateFeatureCount();
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

        public static IEnumerable<Geometry> EnsureSinglePart(Geometry geometry)
        {
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    yield return geometry;
                    break;

                case GeometryType.Polyline:
                    var line = geometry as Polyline;

                    foreach (var part in line.Parts)
                    {
                        yield return new Polyline(part);
                    }
                    break;

                case GeometryType.Polygon:
                    var polygon = geometry as Polygon;

                    foreach (var part in polygon.Parts)
                    {
                        yield return new Polygon(part);
                    }
                    break;

                case GeometryType.Multipoint:
                    foreach (var point in (geometry as Multipoint).Points)
                    {
                        yield return point;
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        public static Geometry RemoveZAndM(Geometry geometry)
        {
            if (!geometry.HasM && !geometry.HasZ)
            {
                return geometry;
            }
            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    var point = geometry as MapPoint;
                    return new MapPoint(point.X, point.Y, geometry.SpatialReference);

                case GeometryType.Polyline:
                case GeometryType.Polygon:
                    List<IEnumerable<MapPoint>> parts = new List<IEnumerable<MapPoint>>();
                    foreach (var part in (geometry as Multipart).Parts)
                    {
                        parts.Add(part.Points.Select(p => RemoveZAndM(p) as MapPoint));
                    }
                    if (geometry.GeometryType == GeometryType.Polyline)
                    {
                        return new Polyline(parts);
                    }
                    else
                    {
                        return new Polygon(parts);
                    }

                case GeometryType.Multipoint:
                    return new Multipoint((geometry as Multipoint).Points.Select(p => RemoveZAndM(p) as MapPoint));

                default:
                    throw new NotSupportedException();
            }
        }
    }
}