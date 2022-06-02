using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.IO;
using MapBoard.Mapping;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGpx = MapBoard.IO.Gpx.Gpx;
using static MapBoard.Model.CoordinateSystem;
using MapBoard.Mapping.Model;
using MapBoard.Model;

namespace MapBoard.IO
{
    public static class Gps
    {
        private const string Filed_Name = "Name";
        private const string Filed_Path = "Path";
        private const string Filed_Date = "Date";
        private const string Filed_Time = "DateTime";
        private const string Filed_Index = "Index";
        private const string Filed_PointIndex = "PIndex";

        /// <summary>
        /// 导入GPX文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type">生成的类型</param>
        public static async Task<ShapefileMapLayerInfo> ImportToNewLayerAsync(string path, GpxImportType type, MapLayerCollection layers, CoordinateSystem baseCs)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            var gpx =await LibGpx.FromFileAsync(path);
            string newName = FileSystem.GetNoDuplicateFile(Path.Combine(FolderPaths.DataPath, name + ".shp"));
            var fields = new List<FieldInfo>()
                {
                    new FieldInfo(Filed_Name,"名称",FieldInfoType.Text),
                    new FieldInfo(Filed_Path,"文件路径",FieldInfoType.Text),
                    new FieldInfo(Filed_Date,"日期",FieldInfoType.Date),
                    new FieldInfo(Filed_Time,"时间",FieldInfoType.Time),
                    new FieldInfo(Filed_Index,"轨迹序号",FieldInfoType.Integer),
                };
            if (type == GpxImportType.Point)
            {
                fields.Add(new FieldInfo(Filed_PointIndex, "点序号", FieldInfoType.Integer));
            }

            var layer = await LayerUtility.CreateShapefileLayerAsync(type == GpxImportType.Point ? GeometryType.Point : GeometryType.Polyline,
                layers, name: Path.GetFileNameWithoutExtension(newName), fields);
            List<Feature> newFeatures = new List<Feature>();
            foreach (var track in gpx.Tracks)
            {
                if (type == GpxImportType.Point)
                {
                    newFeatures.AddRange(ImportAsPoint(track, layer, baseCs));
                }
                else
                {
                    newFeatures.AddRange(ImportAsPolyline(track, layer, baseCs));
                }
            }
            await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.Import);
            return layer;
        }

        private static IEnumerable<Feature> ImportAsPolyline(Gpx.GpxTrack track, IEditableLayerInfo layer, CoordinateSystem baseCs)
        {
            List<MapPoint> points = new List<MapPoint>();
            foreach (var point in track.Points)
            {
                points.Add(CoordinateTransformation.Transformate(point.ToXYMapPoint(), WGS84, baseCs));
            }
            Feature feature = layer.CreateFeature();
            feature.Geometry = new Polyline(points);
            ApplyAttributes(track, layer,null, feature);
            yield return feature;
        }

        private static IEnumerable<Feature> ImportAsPoint(Gpx.GpxTrack track, IEditableLayerInfo layer, CoordinateSystem baseCs)
        {
            int i = 0;
            foreach (var point in track.Points)
            {
                MapPoint mapPoint = CoordinateTransformation.Transformate(point.ToXYMapPoint(), WGS84, baseCs);
                Feature feature = layer.CreateFeature();
                feature.Geometry = mapPoint;
                 ApplyAttributes(track, layer, i++, feature);
                yield return feature;
            }
        }

        private static void ApplyAttributes(Gpx.GpxTrack track, IEditableLayerInfo layer, int? index, Feature feature)
        {
            if (layer.HasField(Filed_Name, FieldInfoType.Text))
            {
                feature.SetAttributeValue(Filed_Name, track.GpxInfo.Name);
                //feature.SetAttributeValue(Filed_Name, Path.GetFileNameWithoutExtension(track.GpxInfo.FilePath));
            }
            if (layer.HasField(Filed_Path, FieldInfoType.Text))
            {
                feature.SetAttributeValue(Filed_Path, track.GpxInfo.FilePath);
            }
            if (layer.HasField(Filed_Date, FieldInfoType.Date))
            {
                feature.SetAttributeValue(Filed_Date, track.GpxInfo.Time);
            }
            if (layer.HasField(Filed_Time, FieldInfoType.Time))
            {
                feature.SetAttributeValue(Filed_Time, track.GpxInfo.Time.ToString(Parameters.TimeFormat));
            }
            if (layer.HasField(Filed_Index, FieldInfoType.Integer))
            {
                feature.SetAttributeValue(Filed_Index, track.GpxInfo.Tracks.IndexOf(track));
            }
            if (layer.HasField(Filed_PointIndex, FieldInfoType.Integer)&&index.HasValue)
            {
                feature.SetAttributeValue(Filed_PointIndex, index.Value);
            }

        }

        public static async Task<ShapefileMapLayerInfo> ImportAllToNewLayerAsync(string[] paths, GpxImportType type, MapLayerCollection layers, CoordinateSystem baseCS)
        {
            var layer = await ImportToNewLayerAsync(paths[0], type, layers, baseCS);
            for (int i = 1; i < paths.Length; i++)
            {
                await ImportToLayerAsync(paths[i], layer, baseCS);
            }
            return layer;
        }

        public static async Task ImportToLayersAsync(IEnumerable<string> paths, IEditableLayerInfo layer, CoordinateSystem baseCS)
        {
            foreach (var path in paths)
            {
                await ImportToLayerAsync(path, layer, baseCS);
            }
        }

        public static async Task<IReadOnlyList<Feature>> ImportToLayerAsync(string path, IEditableLayerInfo layer, CoordinateSystem baseCS)
        {
            var gpx =await LibGpx.FromFileAsync(path);
            List<Feature> importedFeatures = new List<Feature>();

            foreach (var track in gpx.Tracks)
            {
                if (layer.GeometryType == GeometryType.Point)
                {
                    importedFeatures.AddRange(ImportAsPoint(track, layer, baseCS));
                }
                else if (layer.GeometryType == GeometryType.Polyline)
                {
                    importedFeatures.AddRange(ImportAsPolyline(track, layer, baseCS));
                }
                else
                {
                    throw new NotSupportedException("不支持的格式图形类型");
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