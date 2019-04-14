﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Geography.Format;
using FzLib.IO;
using MapBoard.Common;
using MapBoard.Main.Style;
using System;
using System.Collections.Generic;
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
        public async static void Import(string path, Type type)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string content = File.ReadAllText(path);

            var gpx = GpxInfo.FromString(content);

            foreach (var track in gpx.Tracks)
            {
                string newName = FileSystem.GetNoDuplicateFile(Path.Combine(Config.DataPath, name + ".shp"));

                StyleInfo style = StyleHelper.CreateStyle(type == Type.Point ? GeometryType.Point : GeometryType.Polyline, name: Path.GetFileNameWithoutExtension(newName));
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

                style.UpdateFeatureCount();
            }


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
