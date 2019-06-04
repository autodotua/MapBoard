using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Control.Dialog;
using MapBoard.Common.Dialog;
using MapBoard.Main.IO;
using MapBoard.Main.Style;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Helper
{
   public static class FeatureHelper
    {

        public static async void Union(StyleInfo style)
        {
            Geometry geometry = GeometryEngine.Union(ArcMapView.Instance.Selection.SelectedFeatures.Select(p => p.Geometry));
            var firstFeature = ArcMapView.Instance.Selection.SelectedFeatures[0];
            firstFeature.Geometry = geometry;
            await style.Table.UpdateFeatureAsync(firstFeature);
            await style.Table.DeleteFeaturesAsync(ArcMapView.Instance.Selection.SelectedFeatures.Where(p => p != firstFeature));
            ArcMapView.Instance.Selection.ClearSelection();
        }

        public static async void Link(StyleInfo style)
        {
            var features = ArcMapView.Instance.Selection.SelectedFeatures.ToArray();
            List<(string, string, Action)> typeList = new List<(string, string, Action)>();
            int type = 0;
            if (ArcMapView.Instance.Selection.SelectedFeatures.Count == 2)
            {
                typeList.Add(("尾1头——头2尾", "起始点与起始点相连接", () => type = 1));
                typeList.Add(("头1尾——尾2头", "终结点与终结点相连接", () => type = 2));
                typeList.Add(("头1尾——头2尾", "第一个要素的终结点与第二个要素的起始点相连接", () => type = 3));
                typeList.Add(("头2尾——头1尾", "第一个要素的起始点与第二个要素的终结点相连接", () => type = 4));
            }
            else
            {
                typeList.Add(("头n尾——头n+1尾", "每一个要素的终结点与前一个要素的起始点相连接", () => type = 5));
                typeList.Add(("头n尾——头n-1尾", "每一个要素的起始点与前一个要素的终结点相连接", () => type = 6));
            }

            TaskDialog.ShowWithCommandLinks("连接类型", "请选择连接类型", typeList, cancelable: true);

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

        public static async void Reverse(StyleInfo style)
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


        public static async void Densify(StyleInfo style)
        {
            Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];
            NumberInputDialog dialog = new NumberInputDialog("请输入最大间隔");
            if (dialog.ShowDialog() == true)
            {
                feature.Geometry = GeometryEngine.DensifyGeodetic(feature.Geometry, dialog.Number, LinearUnits.Meters);
                await style.Table.UpdateFeatureAsync(feature);
                ArcMapView.Instance.Selection.ClearSelection();
            }
        }

        public static async void RemoveSomePoints(StyleInfo style)
        {
            Feature feature = ArcMapView.Instance.Selection.SelectedFeatures[0];
            NumberInputDialog dialog = new NumberInputDialog("请输入每几个点保留一个点") { Integer = true };
            if (dialog.ShowDialog() == true)
            {
                int eachPoint = dialog.IntNumber;
                if (eachPoint < 2)
                {
                    TaskDialog.ShowError("输入的值不可小于2！");
                    return;
                }

                if (style.Type == GeometryType.Polygon)
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
                await style.Table.UpdateFeatureAsync(feature);

                //feature.Geometry = GeometryEngine.DensifyGeodetic(feature.Geometry, dialog.Number, LinearUnits.Meters);
                //await style.Table.UpdateFeatureAsync(feature);
                //ArcMapView.Instance.Selection.ClearSelection();
            }
        }

        public static void ToCsv(StyleInfo style)
        {
            try
            {
              string path=  Csv.Export(ArcMapView.Instance.Selection.SelectedFeatures);
                if (path != null)
                {
                    SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner);
                    snake.ShowButton = true;
                    snake.ButtonContent = "打开";
                    snake.ButtonClick += (p1, p2) => Process.Start(path);
                    
                    snake.ShowMessage("已导出到" + path);
                }
            }
            catch(Exception ex)
            {
                TaskDialog.ShowException(ex, "导出失败");
            }

        }

        public static async void CreateCopy(StyleInfo style)
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
