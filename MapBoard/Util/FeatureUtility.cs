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
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Util
{
    public static class FeatureUtility
    {
        public static async void Union(LayerInfo style)
        {
            Geometry geometry = GeometryEngine.Union(ArcMapView.Instance.Selection.SelectedFeatures.Select(p => p.Geometry));
            var firstFeature = ArcMapView.Instance.Selection.SelectedFeatures[0];
            firstFeature.Geometry = geometry;
            await style.Table.UpdateFeatureAsync(firstFeature);
            await style.Table.DeleteFeaturesAsync(ArcMapView.Instance.Selection.SelectedFeatures.Where(p => p != firstFeature));
            ArcMapView.Instance.Selection.ClearSelection();
        }

        public static async void Link(LayerInfo style)
        {
            var features = ArcMapView.Instance.Selection.SelectedFeatures.ToArray();
            List<DialogItem> typeList = new List<DialogItem>();
            int type = 0;
            if (ArcMapView.Instance.Selection.SelectedFeatures.Count == 2)
            {
                typeList.Add(new DialogItem("尾1头——头2尾", "起始点与起始点相连接", () => type = 1));
                typeList.Add(new DialogItem("头1尾——尾2头", "终结点与终结点相连接", () => type = 2));
                typeList.Add(new DialogItem("头1尾——头2尾", "第一个要素的终结点与第二个要素的起始点相连接", () => type = 3));
                typeList.Add(new DialogItem("头2尾——头1尾", "第一个要素的起始点与第二个要素的终结点相连接", () => type = 4));
            }
            else
            {
                typeList.Add(new DialogItem("头n尾——头n+1尾", "每一个要素的终结点与前一个要素的起始点相连接", () => type = 5));
                typeList.Add(new DialogItem("头n尾——头n-1尾", "每一个要素的起始点与前一个要素的终结点相连接", () => type = 6));
            }

            await CommonDialog.ShowSelectItemDialogAsync("请选择连接类型", typeList);

            if (type == 0)
            {
                return;
            }
            List<MapPoint> points = null;

            if (type <= 4)
            {
                List<MapPoint> points1 = GetPoints(features[0]);
                List<MapPoint> points2 = GetPoints(features[1]);
                switch (type)
                {
                    case 1:
                        points1.Reverse();
                        points1.AddRange(points2);
                        break;

                    case 2:
                        points2.Reverse();
                        points1.AddRange(points2);
                        break;

                    case 3:
                        points1.AddRange(points2);
                        break;

                    case 4:
                        points1.InsertRange(0, points2);
                        break;
                }
                points = points1;
            }
            else
            {
                IEnumerable<List<MapPoint>> pointsGroup = features.Select(p => GetPoints(p));
                if (type == 6)
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

            await style.Table.UpdateFeatureAsync(features[0]);

            await style.Table.DeleteFeaturesAsync(features.Where(p => p != features[0]));
            ArcMapView.Instance.Selection.ClearSelection();
        }

        public static async void Reverse(LayerInfo style)
        {
            Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];

            Polyline line = feature.Geometry as Polyline;
            List<MapPoint> points = GetPoints(feature);
            points.Reverse();
            //feature.Geometry = new Polyline(points);
            await style.Table.DeleteFeatureAsync(feature);
            await style.Table.AddFeatureAsync(style.Table.CreateFeature(feature.Attributes, new Polyline(points)));
            ArcMapView.Instance.Selection.ClearSelection();
        }

        public static async void Densify(LayerInfo style)
        {
            Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];

            int? num = await CommonDialog.ShowIntInputDialogAsync("请输入最大间隔");
            if (num.HasValue)
            {
                feature.Geometry = GeometryEngine.DensifyGeodetic(feature.Geometry, num.Value, LinearUnits.Meters);
                await style.Table.UpdateFeatureAsync(feature);
                ArcMapView.Instance.Selection.ClearSelection();
            }
        }

        public static async void Simplify(LayerInfo layer)
        {
            Debug.Assert(layer.Type == GeometryType.Polygon || layer.Type == GeometryType.Polyline); ;
            int i = await CommonDialog.ShowSelectItemDialogAsync("请选择简化方法", new DialogItem[]
               {
                new DialogItem("间隔取点法","或每几个点保留一点"),
                new DialogItem("垂距法","中间隔一个点连接两个点，然后计算垂距或角度，在某一个值以内则可以删除中间间隔的点"),
                new DialogItem("分裂法","连接首尾点，计算每一点到连线的垂距，检查是否所有点距离小于限差；若不满足，则保留最大垂距的点，将直线一分为二，递归进行上述操作")
               });
            switch (i)
            {
                case 0:
                    await RemoveSomePoints(layer);
                    break;

                case 1:
                    await VerticalDistanceSimplify(layer);
                    break;

                case 2:
                    await DouglasPeuckerSimplify(layer);
                    break;
            }
        }

        private static async Task VerticalDistanceSimplify(LayerInfo layer)
        {
            Debug.Assert(layer.Type == GeometryType.Polygon || layer.Type == GeometryType.Polyline); ;

            Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];
            double? num = await CommonDialog.ShowFloatInputDialogAsync("请输入最大垂距（米）");

            if (num.HasValue)
            {
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
                    HashSet<MapPoint> deletedPoints = new HashSet<MapPoint>();
                    for (int i = 2; i < part.PointCount; i += 2)
                    {
                        var dist = GetVerticleDistance(part.Points[i - 2], part.Points[i], part.Points[i - 1]);
                        if (dist < num)
                        {
                            deletedPoints.Add(part.Points[i - 1]);
                        }
                    }

                    newParts.Add(part.Points.Except(deletedPoints));
                }
                if (layer.Type == GeometryType.Polygon)
                {
                    feature.Geometry = new Polygon(newParts, feature.Geometry.SpatialReference);
                }
                else if (layer.Type == GeometryType.Polyline)
                {
                    feature.Geometry = new Polyline(newParts, feature.Geometry.SpatialReference);
                }
             await   layer.Table.UpdateFeatureAsync(feature);
            }
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
            var nearestPoint = GeometryEngine.NearestCoordinate(p1P2Line, pc);
            var dist = GeometryEngine.DistanceGeodetic(pc, nearestPoint.Coordinate, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.NormalSection);
            return dist.Distance;
        }

        private static async Task DouglasPeuckerSimplify(LayerInfo layer)
        {
            Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];
            await CommonDialog.ShowErrorDialogAsync("暂未实现");
        }

        public static async Task RemoveSomePoints(LayerInfo layer)
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

        public static void ToCsv(LayerInfo style)
        {
            try
            {
                string path = Csv.Export(ArcMapView.Instance.Selection.SelectedFeatures);
                if (path != null)
                {
                    SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner);
                    snake.ShowButton = true;
                    snake.ButtonContent = "打开";
                    snake.ButtonClick += (p1, p2) => Process.Start(path);

                    snake.ShowMessage("已导出到" + path);
                }
            }
            catch (Exception ex)
            {
                CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
            }
        }

        public static async void CreateCopy(LayerInfo style)
        {
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in ArcMapView.Instance.Selection.SelectedFeatures)
            {
                Feature newFeature = style.Table.CreateFeature(feature.Attributes, feature.Geometry);
                newFeatures.Add(newFeature);
            }
            await style.Table.AddFeaturesAsync(newFeatures);
            ArcMapView.Instance.Selection.ClearSelection();
            ArcMapView.Instance.Selection.Select(newFeatures);
            style.UpdateFeatureCount();
        }

        public static List<MapPoint> GetPoints(Feature feature)
        {
            Polyline line = feature.Geometry as Polyline;
            List<MapPoint> points = new List<MapPoint>();
            foreach (var part in line.Parts)
            {
                points.AddRange(part.Points);
            }
            return points;
        }
    }
}