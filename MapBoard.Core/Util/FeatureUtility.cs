using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static MapBoard.Mapping.Model.FeaturesChangedSource;
using MapBoard.Mapping.Model;
using static MapBoard.Util.GeometryUtility;
using Esri.ArcGISRuntime.Symbology;

namespace MapBoard.Util
{
    public static class FeatureUtility
    {
        /// <summary>
        /// 将多个线、面或多点合并为一个图形
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static async Task<Feature> UnionAsync(IEditableLayerInfo layer, Feature[] features)
        {
            Geometry geometry = null;
            await Task.Run(() =>
            {
                geometry = GeometryEngine.Union(features.Select(p => p.Geometry));
            });
            var firstFeature = features.First();
            var newFeature = layer.CreateFeature(firstFeature.Attributes, geometry);
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeatureAsync(newFeature, FeatureOperation);
            return newFeature;
        }

        /// <summary>
        /// 将多部份的图形分解为单个部分的图形
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<Feature>> SeparateAsync(IEditableLayerInfo layer, Feature[] features)
        {
            List<Feature> deleted = new List<Feature>();
            List<Feature> added = new List<Feature>();
            await Task.Run(() =>
            {
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
            });
            if (added.Count > 0)
            {
                await layer.DeleteFeaturesAsync(deleted, FeatureOperation);
                await layer.AddFeaturesAsync(added, FeatureOperation);
            }
            return added.AsReadOnly();
        }

        /// <summary>
        /// 根据端点之间的距离，自动计算应该连接的端点
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static async Task<Feature> AutoLinkAsync(IEditableLayerInfo layer, Feature[] features)
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
            Feature newFeature = null;
            await Task.Run(() =>
            {
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

                newFeature = layer.CreateFeature(features[0].Attributes, new Polyline(points));
            });
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeatureAsync(newFeature, FeatureOperation);

            return newFeature;
        }

        /// <summary>
        /// 根据参数，连接折线的端点
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="headToHead">是否头和头相连</param>
        /// <param name="reverse">是否反转（尾连头/尾连尾）</param>
        /// <returns></returns>
        public static async Task<Feature> LinkAsync(IEditableLayerInfo layer, Feature[] features, bool headToHead, bool reverse)
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
            Feature newFeature = null;
            await Task.Run(() =>
            {
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

                newFeature = layer.CreateFeature(features[0].Attributes, new Polyline(points));
            });
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeatureAsync(newFeature, FeatureOperation);

            return newFeature;
        }

        /// <summary>
        /// 反转
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static async Task ReverseAsync(IEditableLayerInfo layer, Feature[] features)
        {
            List<UpdatedFeature> updatedFeatures = new List<UpdatedFeature>();
            await Task.Run(() =>
            {
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
                    var oldGeom = feature.Geometry;
                    feature.Geometry = newGeo;
                    updatedFeatures.Add(new UpdatedFeature(feature, oldGeom, feature.Attributes));
                }
            });
            await layer.UpdateFeaturesAsync(updatedFeatures, FeatureOperation);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static async Task DensifyAsync(IEditableLayerInfo layer, Feature[] features, double max)
        {
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();
            await Task.Run(() =>
            {
                foreach (var feature in features.ToList())
                {
                    var newGeo = GeometryEngine.DensifyGeodetic(feature.Geometry, max, LinearUnits.Meters);
                    newFeatures.Add(new UpdatedFeature(feature, feature.Geometry, feature.Attributes));
                    feature.Geometry = newGeo;
                }
            });
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
        }

        /// <summary>
        /// 垂距法简化图形
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static async Task VerticalDistanceSimplifyAsync(IEditableLayerInfo layer, Feature[] features, double max)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);
                newFeatures.Add(await SimplyBaseAsync(layer, feature, part =>
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

        /// <summary>
        /// ArcGIS自带的简化方法（最大偏离法）
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static async Task GeneralizeSimplifyAsync(IEditableLayerInfo layer, Feature[] features, double max)
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

        /// <summary>
        /// 大多数简化方法的抽象
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="feature"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        private static Task<UpdatedFeature> SimplyBaseAsync(IEditableLayerInfo layer, Feature feature,
            Func<ReadOnlyPart, IEnumerable<MapPoint>> func)
        {
            Debug.Assert(layer.GeometryType == GeometryType.Polygon || layer.GeometryType == GeometryType.Polyline); ;
            return Task.Run(() =>
            {
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
            });
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

        /// <summary>
        /// 获取点到线的垂距
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pc"></param>
        /// <returns></returns>
        private static double GetVerticleDistance(Polyline line, MapPoint pc)
        {
            var nearestPoint = GeometryEngine.NearestCoordinate(line, pc);
            var dist = GeometryEngine.DistanceGeodetic(pc, nearestPoint.Coordinate, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.NormalSection);
            return dist.Distance;
        }

        /// <summary>
        /// 分裂法法简化图形
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static async Task DouglasPeuckerSimplifyAsync(IEditableLayerInfo layer, Feature[] features, double max)
        {
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();

            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);

                newFeatures.Add(await SimplyBaseAsync(layer, feature, part =>
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

        /// <summary>
        /// 间隔取点法简化图形
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static async Task IntervalTakePointsSimplifyAsync(IEditableLayerInfo layer, Feature[] features, double interval)
        {
            Dictionary<Feature, Geometry> oldGeometries = new Dictionary<Feature, Geometry>();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();

            if (interval < 2)
            {
                throw new ArgumentOutOfRangeException("间隔不应小于2");
            }
            foreach (var feature in features)
            {
                oldGeometries.Add(feature, feature.Geometry);

                newFeatures.Add(await SimplyBaseAsync(layer, feature, part =>
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

        /// <summary>
        /// 创建副本
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<Feature>> CreateCopyAsync(IEditableLayerInfo layer, Feature[] features)
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

        /// <summary>
        /// 删除图形
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public static async Task DeleteAsync(IEditableLayerInfo layer, Feature[] features)
        {
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
        }

        /// <summary>
        /// 切割图形
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="clipLine"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<Feature>> CutAsync(IEditableLayerInfo layer, Feature[] features, Polyline clipLine)
        {
            List<Feature> added = new List<Feature>();
            await Task.Run(() =>
            {
                foreach (var feature in features)
                {
                    var newGeos = GeometryEngine.Cut(feature.Geometry,
                        GeometryEngine.Project(clipLine, SpatialReferences.Wgs84) as Polyline);
                    int count = 0;
                    foreach (var newGeo in newGeos)
                    {
                        //对于面，由于有“洞”的存在，不能把不同部分拆分
                        if (layer.GeometryType == GeometryType.Polygon)
                        {
                            count++;
                            Feature newFeature = layer.CreateFeature(feature.Attributes, newGeo);
                            added.Add(newFeature);
                        }
                        //对于线，可以每个部分单独成为一个图形
                        else if (layer.GeometryType == GeometryType.Polyline)
                        {
                            foreach (var part in (newGeo as Multipart).Parts)
                            {
                                count++;
                                Feature newFeature = layer.CreateFeature(feature.Attributes, new Polyline(part));
                                added.Add(newFeature);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
            });
            await layer.DeleteFeaturesAsync(features, FeatureOperation);
            await layer.AddFeaturesAsync(added, FeatureOperation);
            return added;
        }

        /// <summary>
        /// 复制或移动图形到另一个图层
        /// </summary>
        /// <param name="layerFrom"></param>
        /// <param name="layerTo"></param>
        /// <param name="features"></param>
        /// <param name="copy"></param>
        /// <returns></returns>
        public static async Task<IReadOnlyList<Feature>> CopyOrMoveAsync(IMapLayerInfo layerFrom, IEditableLayerInfo layerTo, Feature[] features, bool copy)
        {
            var newFeatures = new List<Feature>();
            var fields = layerTo.Fields.Select(p => p.Name).ToHashSet();
            await Task.Run(() =>
            {
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
            });
            await layerTo.AddFeaturesAsync(newFeatures, FeatureOperation);
            if (!copy && layerFrom is IEditableLayerInfo w)
            {
                await DeleteAsync(w, features);
            }
            layerFrom.LayerVisible = false;
            layerTo.LayerVisible = true;
            return newFeatures.AsReadOnly();
        }

        /// <summary>
        /// 将一个要素的图形转换成点
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
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
                throw new NotSupportedException("不支持操作拥有多个部分的要素");
            }
            List<MapPoint> points = new List<MapPoint>();
            foreach (var part in parts)
            {
                points.AddRange(part.Points);
            }
            return points;
        }

        /// <summary>
        /// 将图形建立缓冲区后放入一个多边形图层
        /// </summary>
        /// <returns></returns>
        public static async Task SimpleBufferToLayerAsync(IMapLayerInfo layerFrom, IEditableLayerInfo layerTo, Feature[] features, double meters)
        {
            List<Feature> newFeatures = new List<Feature>();
            await Task.Run(() =>
            {
                foreach (var feature in features)
                {
                    Geometry oldGeometry = GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator);
                    var geometry = GeometryEngine.Buffer(oldGeometry, meters);
                    Feature newFeature = layerTo.CreateFeature(feature.Attributes, geometry);
                    newFeatures.Add(newFeature);
                }
            });
            await layerTo.AddFeaturesAsync(newFeatures, FeatureOperation);
        }

        /// <summary>
        /// 将图形建立缓冲区后放入一个多边形图层
        /// </summary>
        /// <returns></returns>
        public static async Task BufferToLayerAsync(IMapLayerInfo layerFrom, IEditableLayerInfo layerTo, Feature[] features, double meters, bool union)
        {
            List<Feature> newFeatures = new List<Feature>();
            await Task.Run(() =>
            {
                var geometries = GeometryEngine.BufferGeodetic(features.Select(p => p.Geometry), new double[] { meters }, LinearUnits.Meters, unionResult: union).ToList();
                if (!union)
                {
                    Debug.Assert(features.Length == geometries.Count);
                    for (int i = 0; i < features.Length; i++)
                    {
                        Feature newFeature = layerTo.CreateFeature(features[i].Attributes, geometries[i]);
                        newFeatures.Add(newFeature);
                    }
                }
                else
                {
                    Debug.Assert(geometries.Count == 1);
                    Feature newFeature = layerTo.CreateFeature(null, geometries[0]);
                    newFeatures.Add(newFeature);
                }
            });
            await layerTo.AddFeaturesAsync(newFeatures, FeatureOperation);
        }

        /// <summary>
        /// 平滑
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="features"></param>
        /// <param name="pointsPerSegment">两个节点之间生成多少新点</param>
        /// <param name="level">平滑等级，0最拟合，1一般，2最平滑</param>
        /// <returns></returns>
        public static async Task Smooth(IEditableLayerInfo layer, Feature[] features, int pointsPerSegment, int level)
        {
            if (level < 0 || level > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }
            if (pointsPerSegment <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pointsPerSegment));
            }
            //CentripetalCatmullRom中数量是算上头尾的
            pointsPerSegment += 2;
            List<UpdatedFeature> updatedFeatures = new List<UpdatedFeature>();
            foreach (var feature in features)
            {
                Geometry oldGeometry = feature.Geometry;
                Geometry newGeometry = null;
                if (feature.Geometry is Multipart m)
                {
                    List<IEnumerable<MapPoint>> newParts = new List<IEnumerable<MapPoint>>();
                    foreach (var part in m.Parts)
                    {
                        if (part.PointCount <= 2)
                        {
                            newParts.Add(part.Points);
                        }
                        else
                        {
                            newParts.Add(CentripetalCatmullRom.Interpolate(part.Points.ToList(), pointsPerSegment, (CentripetalCatmullRom.CatmullRomType)level));
                        }
                    }
                    switch (feature.Geometry)
                    {
                        case Polyline line:
                            newGeometry = new Polyline(newParts);
                            break;

                        case Polygon polygon:
                            newGeometry = new Polygon(newParts);
                            break;
                    }
                }
                else if (feature.Geometry is Multipoint mp)
                {
                    if (mp.Points.Count > 2)
                    {
                        newGeometry = new Multipoint(CentripetalCatmullRom.Interpolate(mp.Points.ToList(), pointsPerSegment, (CentripetalCatmullRom.CatmullRomType)level));
                    }
                }
                else
                {
                    throw new NotSupportedException("仅支持多点、折线和多边形");
                }
                feature.Geometry = newGeometry;
                updatedFeatures.Add(new UpdatedFeature(feature, oldGeometry, feature.Attributes));
            }
            await layer.UpdateFeaturesAsync(updatedFeatures, FeatureOperation);
        }
    }
}