using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.IO;
using MapBoard.Common;
using MapBoard.Main.Helper;
using MapBoard.Main.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
{
    public static class Gpx
    {
        /// <summary>
        /// 导入GPX文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type">生成的类型</param>
        public async static Task<StyleInfo> ImportToNewStyle(string path, Type type)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path);

            var gpx = GIS.IO.Gpx.Gpx.FromString(content);
            string newName = FileSystem.GetNoDuplicateFile(Path.Combine(Config.DataPath, name + ".shp"));

            StyleInfo style = StyleHelper.CreateStyle(type == Type.Point ? GeometryType.Point : GeometryType.Polyline, name: Path.GetFileNameWithoutExtension(newName));

            foreach (var track in gpx.Tracks)
            {

                FeatureTable table = style.Table;
                CoordinateTransformation transformation = new CoordinateTransformation("WGS84", Config.Instance.BasemapCoordinateSystem);


                if (type == Type.Point)
                {
                    List<Feature> features = new List<Feature>();
                    foreach (var point in track.Points)
                    {
                        MapPoint TransformateToMapPoint = transformation.TransformateToMapPoint(point);
                        Feature feature = table.CreateFeature();
                        feature.Geometry = TransformateToMapPoint;
                        features.Add(feature);
                    }
                    await table.AddFeaturesAsync(features);
                }
                else if (type == Type.MultiLine)
                {
                    MapPoint lastPoint = null;
                    List<Feature> features = new List<Feature>();
                    foreach (var point in track.Points)
                    {
                        if (lastPoint == null)
                        {
                            lastPoint = transformation.TransformateToMapPoint(point);
                        }
                        else
                        {
                            var newPoint = transformation.TransformateToMapPoint(point);
                            Feature feature = table.CreateFeature();
                            feature.Geometry = new Polyline(new MapPoint[] { lastPoint, newPoint });
                            features.Add(feature);
                            lastPoint = newPoint;
                        }
                    }

                    await table.AddFeaturesAsync(features);
                }
                else
                {
                    List<MapPoint> points = new List<MapPoint>();
                    foreach (var point in track.Points)
                    {
                        points.Add(transformation.TransformateToMapPoint(point));
                    }
                    Feature feature = table.CreateFeature();
                    feature.Geometry = new Polyline(points);
                    await table.AddFeatureAsync(feature);
                }

            }
            style.UpdateFeatureCount();
            return style;

        }

        public async static Task<StyleInfo> ImportAllToNewStyle(string[] paths)
        {
            Debug.Assert(paths.Length >= 2);
            var style = await ImportToNewStyle(paths[0], Type.OneLine);
            for(int i=1;i<paths.Length;i++)
            {
                await ImportToStyle(style, paths[i]);
            }
            return style;
        }

        public async static Task<Feature[]> ImportToStyle(StyleInfo style, string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path);

            var gpx = GIS.IO.Gpx.Gpx.FromString(content);
            FeatureTable table = style.Table;
            CoordinateTransformation transformation = new CoordinateTransformation("WGS84", Config.Instance.BasemapCoordinateSystem);
            List<Feature> importedFeatures = new List<Feature>();

            foreach (var track in gpx.Tracks)
            {


                if (style.Type == GeometryType.Point)
                {
                    List<Feature> features = new List<Feature>();
                    foreach (var point in track.Points)
                    {
                        MapPoint TransformateToMapPoint = transformation.TransformateToMapPoint(point);
                        Feature feature = table.CreateFeature();
                        feature.Geometry = TransformateToMapPoint;
                        features.Add(feature);
                    }
                    importedFeatures = features;
                    await table.AddFeaturesAsync(features);
                }
                else if (style.Type == GeometryType.Multipoint)
                {
                    //IEnumerable<MapPoint> points = track.Points.Select(p => transformation.TransformateToMapPoint(p));
                    //Feature feature = table.CreateFeature();
                    //feature.Geometry = new Multipoint(points);
                    //await table.AddFeatureAsync(feature);

                    throw new Exception("由于内部BUG，多点暂不支持导入GPX");

                }
                else if (style.Type == GeometryType.Polyline)
                {
                    var points = track.Points.Select(p => transformation.TransformateToMapPoint(p));
                    Feature feature = table.CreateFeature();
                    feature.Geometry = new Polyline(points, transformation.ToSpatialReference);
                    await table.AddFeatureAsync(feature);
                    importedFeatures.Add(feature);
                }
                else
                {
                    throw new Exception("不支持的格式图形类型");
                }

            }

            style.UpdateFeatureCount();
            return importedFeatures.ToArray();
        }
        public async static Task<Feature[]> ImportToCurrentLayer(string path)
        {
            StyleInfo style = StyleCollection.Instance.Selected;
            return await ImportToStyle(style, path);
        }


        /// <summary>
        /// 生成的图形的类型
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// 每一个点就是一个点
            /// </summary>
            Point,
            /// <summary>
            /// 连点成线，所有点生成一条线
            /// </summary>
            OneLine,
            /// <summary>
            /// 每两个点之间生成一条线
            /// </summary>
            MultiLine
        }
    }
}
