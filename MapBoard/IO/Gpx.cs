using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.IO;
using MapBoard.Common;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGpx = FzLib.Geography.IO.Gpx.Gpx;

namespace MapBoard.Main.IO
{
    public static class Gpx
    {
        /// <summary>
        /// 导入GPX文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type">生成的类型</param>
        public async static Task<LayerInfo> ImportToNewLayerAsync(string path, GpxImportType type, MapLayerCollection layers)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path);

            var gpx = LibGpx.FromString(content);
            string newName = FileSystem.GetNoDuplicateFile(Path.Combine(Config.DataPath, name + ".shp"));

            LayerInfo layer = await LayerUtility.CreateLayerAsync(type == GpxImportType.Point ? GeometryType.Point : GeometryType.Polyline,
                layers, name: Path.GetFileNameWithoutExtension(newName));

            foreach (var track in gpx.Tracks)
            {
                FeatureTable table = layer.Table;
                CoordinateTransformation transformation = new CoordinateTransformation("WGS84", Config.Instance.BasemapCoordinateSystem);

                if (type == GpxImportType.Point)
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
            layer.NotifyFeatureChanged();
            return layer;
        }

        public async static Task<LayerInfo> ImportAllToNewLayerAsync(string[] paths, GpxImportType type, MapLayerCollection layers)
        {
            var layer = await ImportToNewLayerAsync(paths[0], type, layers);
            for (int i = 1; i < paths.Length; i++)
            {
                await ImportToLayerAsync(paths[i], layer);
            }
            return layer;
        }

        public async static Task ImportToLayersAsync(IEnumerable<string> paths, LayerInfo layer)
        {
            foreach (var path in paths)
            {
                await ImportToLayerAsync(path, layer);
            }
        }

        public async static Task<Feature[]> ImportToLayerAsync(string path, LayerInfo layer)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path);

            var gpx = LibGpx.FromString(content);
            FeatureTable table = layer.Table;
            CoordinateTransformation transformation = new CoordinateTransformation("WGS84", Config.Instance.BasemapCoordinateSystem);
            List<Feature> importedFeatures = new List<Feature>();

            foreach (var track in gpx.Tracks)
            {
                if (layer.Table.GeometryType == GeometryType.Point)
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
                else if (layer.Table.GeometryType == GeometryType.Multipoint)
                {
                    throw new Exception("多点暂不支持导入GPX");
                }
                else if (layer.Table.GeometryType == GeometryType.Polyline)
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

            layer.NotifyFeatureChanged();
            return importedFeatures.ToArray();
        }

        /// <summary>
        /// 生成的图形的类型
        /// </summary>
        public enum GpxImportType
        {
            /// <summary>
            /// 每一个点就是一个点
            /// </summary>
            Point,

            /// <summary>
            /// 连点成线，所有点生成一条线
            /// </summary>
            Line,
        }
    }
}