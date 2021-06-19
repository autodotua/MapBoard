using EGIS.ShapeFileLib;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;

using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;

namespace MapBoard.IO
{
    public static class Shapefile
    {
        /// <summary>
        /// shapefile的可能的文件扩展名
        /// </summary>
        public static readonly string[] ShapefileExtensions = new string[]
        {
            ".shp",
            ".shx",
            ".dbf",
            ".prj",
            ".xml",
            ".shp.xml",
            ".cpg",
            ".sbn",
            ".sbx",
        };

        /// <summary>
        /// 获取某一个无扩展名的shapefile的名称可能对应的shapefile的所有文件
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetExistShapefiles(string directory, string name)
        {
            if (name.EndsWith(".shp"))
            {
                name = name.RemoveEnd(".shp");
            }
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (Path.GetFileNameWithoutExtension(file) == name)
                {
                    if (ShapefileExtensions.Contains(Path.GetExtension(file)))
                    {
                        yield return file;
                    }
                }
            }
        }

        /// <summary>
        /// 导入shapefile文件
        /// </summary>
        /// <param name="path"></param>
        public async static Task ImportAsync(string path, MapLayerCollection layers)
        {
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            bool label = table.Fields.Any(p => p.Name == Parameters.LabelFieldName && p.FieldType == FieldType.Text);
            bool date = table.Fields.Any(p => p.Name == Parameters.DateFieldName && p.FieldType == FieldType.Date);
            bool key = table.Fields.Any(p => p.Name == Parameters.ClassFieldName && p.FieldType == FieldType.Text);
            FeatureQueryResult features = await table.QueryFeaturesAsync(new QueryParameters());

            MapLayerInfo layer = await LayerUtility.CreateLayerAsync(table.GeometryType, layers,
                null, Path.GetFileNameWithoutExtension(path),
                table.Fields.FromEsriFields().ToList());
            layer.LayerVisible = false;
            var fields = layer.Fields.Select(p => p.Name).ToHashSet();
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in features)
            {
                Dictionary<string, object> newAttributes = new Dictionary<string, object>();
                foreach (var attr in feature.Attributes)
                {
                    if (attr.Key.ToLower() == "id")
                    {
                        continue;
                    }
                    if (fields.Contains(attr.Key))
                    {
                        newAttributes.Add(attr.Key, attr.Value);
                    }
                }
                Feature newFeature = layer.CreateFeature(newAttributes, GeometryUtility.RemoveZAndM(feature.Geometry));
                newFeatures.Add(newFeature);
            }
            await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.Import);

            layer.LayerVisible = true;
        }

        public async static Task<ShapefileFeatureTable> CreateShapefileAsync(GeometryType type, string name, string folder = null, IEnumerable<FieldInfo> fields = null)
        {
            if (folder == null)
            {
                folder = Parameters.DataPath;
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string path = Path.Combine(folder, name);
            if (fields == null)
            {
                fields = FieldExtension.DefaultFields;
            }
            else
            {
                fields = fields
                    .Where(p => p.Name.ToLower() != "fid")
                    .Where(p => p.Name.ToLower() != "id")
                    .IncludeDefaultFields();
            }

            CreateEgisShapefile(type, name, folder, fields);
            path = path + ".shp";
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            return table;
        }

        public static void CreateEgisShapefile(GeometryType type, string name, string folder, IEnumerable<FieldInfo> fields)
        {
            ShapeType egisType;
            switch (type)
            {
                case GeometryType.Point:
                    egisType = ShapeType.Point;
                    break;

                case GeometryType.Polyline:
                    egisType = ShapeType.PolyLine;
                    break;

                case GeometryType.Polygon:
                    egisType = ShapeType.Polygon;
                    break;

                case GeometryType.Multipoint:
                    egisType = ShapeType.Point;
                    break;

                default:
                    throw new NotSupportedException();
            }

            List<DbfFieldDesc> egisFields = new List<DbfFieldDesc>();
            foreach (var field in fields)
            {
                DbfFieldType fieldType = default;
                int decimalCount = 0;
                switch (field.Type)
                {
                    case FieldInfoType.Integer:
                        fieldType = DbfFieldType.Number;
                        break;

                    case FieldInfoType.Float:
                        fieldType = DbfFieldType.Number;
                        decimalCount = 6;
                        break;

                    case FieldInfoType.Date:
                        fieldType = DbfFieldType.Date;
                        break;

                    case FieldInfoType.Text:
                    case FieldInfoType.Time:
                        fieldType = DbfFieldType.Character;
                        break;
                }
                var f = new DbfFieldDesc()
                {
                    FieldLength = field.Type.GetLength(),
                    FieldName = field.Name,
                    FieldType = fieldType,
                    DecimalCount = decimalCount
                };
                egisFields.Add(f);
            }
            using ShapeFileWriter sfw = ShapeFileWriter.CreateWriter(folder, name, egisType, egisFields.ToArray());
            sfw.Close();
            //EGIS无法创建多点，因此创建点以后手动修改类型
            //类型位于shape文件的[32..35]，大端序，点、折线、多边形、多点分别为1、3、5、8
            if (type == GeometryType.Multipoint)
            {
                var shpPath = Path.Combine(folder, name + ".shp");
                var bytes = File.ReadAllBytes(shpPath);
                var current = bytes[32];
                if (current != 1)
                {
                    throw new Exception("类型字节应为1，实际为" + current);
                }
                bytes[32] = 8;
                File.WriteAllBytes(shpPath, bytes);
            }
            File.WriteAllText(Path.Combine(folder, name + ".prj"), SpatialReferences.Wgs84.WkText);
            File.WriteAllText(Path.Combine(folder, name + ".cpg"), "UTF-8");
        }

        public static async Task<string> CloneFeatureToNewShpAsync(string directory, MapLayerInfo layer)
        {
            var table = await CreateShapefileAsync(layer.GeometryType, layer.Name, directory, layer.Fields);
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in await layer.GetAllFeaturesAsync())
            {
                newFeatures.Add(
                    table.CreateFeature(
                        feature.Attributes.Where(p => p.Key.ToLower() != "fid" && p.Key.ToLower() != "id"), feature.Geometry));
            }
            await table.AddFeaturesAsync(newFeatures);
            table.Close();
            return table.Path;
        }
    }
}