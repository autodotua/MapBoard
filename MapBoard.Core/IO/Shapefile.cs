using EGIS.ShapeFileLib;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Text.RegularExpressions;
using FzLib;

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
        /// 将图层克隆并保存到新的目录中的shapefile
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static async Task CloneFeatureToNewShpAsync(string directory, ShapefileMapLayerInfo layer)
        {
            var table = await CreateShapefileAsync(layer.GeometryType, layer.Name, directory, layer.Fields);
            List<Feature> newFeatures = new List<Feature>();
            Feature[] features = await layer.GetAllFeaturesAsync();
            try
            {
                foreach (var feature in features)
                {
                    newFeatures.Add(
                        table.CreateFeature(
                            feature.Attributes.Where(p => !FieldExtension.IsIdField(p.Key)),
                            feature.Geometry));
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            await table.AddFeaturesAsync(newFeatures);
            table.Close();
        }

        /// <summary>
        /// 创建Shapefile文件
        /// </summary>
        /// <param name="type">几何类型</param>
        /// <param name="name">Shapefile文件名</param>
        /// <param name="folder">目标目录</param>
        /// <param name="fields">字段</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<ShapefileFeatureTable> CreateShapefileAsync(GeometryType type, string name, string folder = null, IEnumerable<FieldInfo> fields = null)
        {
            //如果没有指定目录则使用数据目录
            folder ??= FolderPaths.DataPath;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string path = Path.Combine(folder, name);

            //排除由ArcGIS自动创建的临时字段，判断字段合法性
            fields = fields
                .Where(p => !p.IsIdField())//ID
                .Where(p => p.Name.ToLower() != "shape_leng")//长度
                .Where(p => p.Name.ToLower() != "shape_area");//面积
            if (fields.Any(field => !Regex.IsMatch(field.Name[0].ToString(), "[a-zA-Z]")
                  || !Regex.IsMatch(field.Name, "^[a-zA-Z0-9_]+$")))
            {
                throw new ArgumentException($"字段名存在不合法");
            }
            //创建文件
            await CreateEgisShapefileAsync(type, name, folder, fields);
            path += ".shp";
            //使用ArcGIS加载文件
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            return table;
        }

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
            return Directory.EnumerateFiles(directory)
                 .Where(p => Path.GetFileNameWithoutExtension(p) == name)
                 .Where(p => ShapefileExtensions.Contains(Path.GetExtension(p)));
        }

        /// <summary>
        /// 导入shapefile文件到新图层
        /// </summary>
        /// <param name="path"></param>
        public static async Task ImportAsync(string path, MapLayerCollection layers)
        {
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            FeatureQueryResult features = await table.QueryFeaturesAsync(new QueryParameters());
            var fieldMap = table.Fields.FromEsriFields();//从原表字段名到新字段的映射
            ShapefileMapLayerInfo layer = await LayerUtility.CreateShapefileLayerAsync(table.GeometryType, layers,
                 Path.GetFileNameWithoutExtension(path),
                fieldMap.Values.ToList());
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
                    string name = attr.Key;//现在是源文件的字段名

                    if (!fieldMap.ContainsKey(name))
                    {
                        continue;
                    }
                    name = fieldMap[name].Name;//切换到目标表的字段名

                    object value = attr.Value;
                    if (value is short)
                    {
                        value = Convert.ToInt32(value);
                    }
                    else if (value is float)
                    {
                        value = Convert.ToDouble(value);
                    }
                    newAttributes.Add(name, value);
                }
                Feature newFeature = layer.CreateFeature(newAttributes, feature.Geometry.RemoveZAndM());
                newFeatures.Add(newFeature);
            }
            await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.Import);

            layer.LayerVisible = true;
        }

        /// <summary>
        /// 调用EGIS库，来创建一个Shapefile
        /// </summary>
        /// <param name="type">几何类型</param>
        /// <param name="name">Shapefile文件名</param>
        /// <param name="folder">目标目录</param>
        /// <param name="fields">字段</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="Exception"></exception>
        private static async Task CreateEgisShapefileAsync(GeometryType type, string name, string folder, IEnumerable<FieldInfo> fields)
        {
            //转换为EGIS几何类型
            var egisType = type switch
            {
                GeometryType.Point => ShapeType.Point,
                GeometryType.Polyline => ShapeType.PolyLine,
                GeometryType.Polygon => ShapeType.Polygon,
                GeometryType.Multipoint => ShapeType.Point,
                _ => throw new NotSupportedException(),
            };
            //转换为EGIS字段
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
            //创建Shapefile
            using ShapeFileWriter sfw = ShapeFileWriter.CreateWriter(folder, name, egisType, egisFields.ToArray());
            sfw.Close();
            //EGIS无法创建多点，因此创建点以后手动修改类型
            //类型位于shape文件的[32..35]，大端序，点、折线、多边形、多点分别为1、3、5、8
            if (type == GeometryType.Multipoint)
            {
                var shpPath = Path.Combine(folder, name + ".shp");
                var bytes = await File.ReadAllBytesAsync(shpPath);
                var current = bytes[32];
                if (current != 1)
                {
                    throw new Exception("类型字节应为1，实际为" + current);
                }
                bytes[32] = 8;
                await File.WriteAllBytesAsync(shpPath, bytes);
            }
            //写入投影信息和编码信息
            await File.WriteAllTextAsync(Path.Combine(folder, name + ".prj"), SpatialReferences.Wgs84.WkText);
            await File.WriteAllTextAsync(Path.Combine(folder, name + ".cpg"), "UTF-8");
        }
    }
}