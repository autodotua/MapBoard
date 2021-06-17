﻿using Esri.ArcGISRuntime.Data;
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
using static MapBoard.Common.CoordinateSystem;
using MapBoard.Main.UI.Model;

namespace MapBoard.Main.IO
{
    public static class Gpx
    {
        /// <summary>
        /// 导入GPX文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type">生成的类型</param>
        public async static Task<MapLayerInfo> ImportToNewLayerAsync(string path, GpxImportType type, MapLayerCollection layers)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path);

            var gpx = LibGpx.FromString(content);
            string newName = FileSystem.GetNoDuplicateFile(Path.Combine(Parameters.DataPath, name + ".shp"));

            MapLayerInfo layer = await LayerUtility.CreateLayerAsync(type == GpxImportType.Point ? GeometryType.Point : GeometryType.Polyline,
                layers, name: Path.GetFileNameWithoutExtension(newName));
            List<Feature> newFeatures = new List<Feature>();
            foreach (var track in gpx.Tracks)
            {

                if (type == GpxImportType.Point)
                {
                    foreach (var point in track.Points)
                    {
                        MapPoint TransformateToMapPoint = CoordinateTransformation.TransformateToMapPoint(point, WGS84, Config.Instance.BasemapCoordinateSystem);
                        Feature feature = layer.CreateFeature();
                        feature.Geometry = TransformateToMapPoint;
                        newFeatures.Add(feature);
                    }
                }
                else
                {
                    List<MapPoint> points = new List<MapPoint>();
                    foreach (var point in track.Points)
                    {
                        points.Add(CoordinateTransformation.TransformateToMapPoint(point, WGS84, Config.Instance.BasemapCoordinateSystem));
                    }
                    Feature feature = layer.CreateFeature();
                    feature.Geometry = new Polyline(points);
                    newFeatures.Add(feature);
                }
            }
            await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.Import);
            return layer;
        }

        public async static Task<MapLayerInfo> ImportAllToNewLayerAsync(string[] paths, GpxImportType type, MapLayerCollection layers)
        {
            var layer = await ImportToNewLayerAsync(paths[0], type, layers);
            for (int i = 1; i < paths.Length; i++)
            {
                await ImportToLayerAsync(paths[i], layer);
            }
            return layer;
        }

        public async static Task ImportToLayersAsync(IEnumerable<string> paths, MapLayerInfo layer)
        {
            foreach (var path in paths)
            {
                await ImportToLayerAsync(path, layer);
            }
        }

        public async static Task<IReadOnlyList<Feature>> ImportToLayerAsync(string path, MapLayerInfo layer)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path);

            var gpx = LibGpx.FromString(content);
            List<Feature> importedFeatures = new List<Feature>();

            foreach (var track in gpx.Tracks)
            {
                if (layer.GeometryType == GeometryType.Point)
                {
                    List<Feature> features = new List<Feature>();
                    foreach (var point in track.Points)
                    {
                        MapPoint TransformateToMapPoint = CoordinateTransformation.TransformateToMapPoint(point, WGS84, Config.Instance.BasemapCoordinateSystem);
                        Feature feature = layer.CreateFeature();
                        feature.Geometry = TransformateToMapPoint;
                        features.Add(feature);
                    }
                    importedFeatures = features;
                    await layer.AddFeaturesAsync(features, FeaturesChangedSource.Import);
                }
                else if (layer.GeometryType == GeometryType.Multipoint)
                {
                    throw new Exception("多点暂不支持导入GPX");
                }
                else if (layer.GeometryType == GeometryType.Polyline)
                {
                    var points = track.Points.Select(p => CoordinateTransformation.TransformateToMapPoint(p, WGS84, Config.Instance.BasemapCoordinateSystem));
                    Feature feature = layer.CreateFeature();
                    feature.Geometry = new Polyline(points, SpatialReferences.Wgs84);
                    importedFeatures.Add(feature);
                }
                else
                {
                    throw new Exception("不支持的格式图形类型");
                }
            }
            await layer.AddFeaturesAsync(importedFeatures, FeaturesChangedSource.Import);

            return importedFeatures.AsReadOnly();
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