using EGIS.ShapeFileLib;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using MapBoard.Common;

using MapBoard.Main.Model;
using MapBoard.Main.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
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
        public async static Task ImportAsync(string path)
        {
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            bool label = table.Fields.Any(p => p.Name == Resource.LabelFieldName && p.FieldType == FieldType.Text);
            bool date = table.Fields.Any(p => p.Name == Resource.DateFieldName && p.FieldType == FieldType.Date);
            bool key = table.Fields.Any(p => p.Name == Resource.ClassFieldName && p.FieldType == FieldType.Text);
            FeatureQueryResult features = await table.QueryFeaturesAsync(new QueryParameters());

            LayerInfo layer = await LayerUtility.CreateLayerAsync(table.GeometryType,
                null, Path.GetFileNameWithoutExtension(path),
                table.Fields.ToFieldInfos().ToList());
            layer.LayerVisible = false;
            var fields = layer.Table.Fields.Select(p => p.Name).ToHashSet();
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
                Feature newFeature = layer.Table.CreateFeature(newAttributes, GeometryUtility.RemoveZAndM(feature.Geometry));

                await layer.Table.AddFeatureAsync(newFeature);
            }

            layer.NotifyFeatureChanged();
            layer.LayerVisible = true;
        }

        public async static Task<ShapefileFeatureTable> CreateShapefileAsync(GeometryType type, string name, string folder = null, IEnumerable<FieldInfo> fields = null)
        {
            if (folder == null)
            {
                folder = Config.DataPath;
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string path = Path.Combine(folder, name);
            if (fields == null)
            {
                fields = FieldUtility.GetDefaultFields();
            }
            else
            {
                fields = fields
                    .Where(p => p.Name.ToLower() != "fid")
                    .Where(p => p.Name.ToLower() != "id")
                    .IncludeDefaultFields();
            }

            //NtsShapefile.CreateShapefile(path, type, esriFields);
            CreateShapefile(type, name, folder, fields);
            path = path + ".shp";
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            //var feature = (await table.QueryFeaturesAsync(new QueryParameters())).First();
            //await table.DeleteFeatureAsync(feature);
            return table;
        }

        public static void CreateShapefile(GeometryType type, string name, string folder, IEnumerable<FieldInfo> fields)
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
                    egisType = ShapeType.MultiPoint;
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
            File.WriteAllText(Path.Combine(folder, name + ".prj"), SpatialReferences.Wgs84.WkText);
        }

        public static async Task<string> CloneFeatureToNewShpAsync(string directory, LayerInfo layer)
        {
            var table = await CreateShapefileAsync(layer.Table.GeometryType, layer.Name, directory, layer.Fields);
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