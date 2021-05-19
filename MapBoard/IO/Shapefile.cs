using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using FzLib.UI.Dialog;
using MapBoard.Common;

using MapBoard.Common;

using MapBoard.Main.Model;
using MapBoard.Main.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            LayerInfo layer = await LayerUtility.CreateLayerAsync(table.GeometryType, null, Path.GetFileNameWithoutExtension(path));
            foreach (var feature in features)
            {
                Feature newFeature = layer.Table.CreateFeature();
                newFeature.Geometry = GeometryUtility.RemoveZAndM(feature.Geometry);
                if (label)
                {
                    newFeature.Attributes[Resource.LabelFieldName] = feature.Attributes[Resource.LabelFieldName];
                }
                if (date)
                {
                    newFeature.Attributes[Resource.DateFieldName] = feature.Attributes[Resource.DateFieldName];
                }
                if (key)
                {
                    newFeature.Attributes[Resource.ClassFieldName] = feature.Attributes[Resource.ClassFieldName];
                }

                await layer.Table.AddFeatureAsync(newFeature);
            }
            layer.UpdateFeatureCount();
        }

        public async static Task<ShapefileFeatureTable> CreateShapefileAsync(GeometryType type, string name, string folder = null, IList<Field> fields = null)
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
                fields = new List<Field>()
                {
                //   new Field(FieldType.OID,"Id",null,9),
                    new Field(FieldType.Text,Resource.LabelFieldName,null,254),
                    new Field(FieldType.Date,Resource.DateFieldName,null,8),
                    new Field(FieldType.Text,Resource.ClassFieldName,null,254),
                };
            }
            else
            {
                fields = fields.Where(p => p.Name.ToLower() != "fid").Where(p => p.Name.ToLower() != "id").ToList();
                if (!fields.Any(p => p.Name == Resource.LabelFieldName))
                {
                    fields.Add(new Field(FieldType.Text, Resource.LabelFieldName, null, 254));
                }
                if (!fields.Any(p => p.Name == Resource.ClassFieldName))
                {
                    fields.Add(new Field(FieldType.Text, Resource.ClassFieldName, null, 254));
                }
                if (!fields.Any(p => p.Name == Resource.DateFieldName))
                {
                    fields.Add(new Field(FieldType.Date, Resource.DateFieldName, null, 8));
                }
            }

            NtsShapefile.CreateShapefile(path, type, fields.ToArray());
            path = path + ".shp";
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            var feature = (await table.QueryFeaturesAsync(new QueryParameters())).First();
            await table.DeleteFeatureAsync(feature);
            return table;
        }

        public static async Task<string> CloneFeatureToNewShpAsync(string directory, LayerInfo layer)
        {
            var table = await CreateShapefileAsync(layer.Type, layer.Name, directory, layer.Table.Fields.ToList());
            List<Feature> newFeatures = new List<Feature>();
            foreach (var feature in await layer.GetAllFeatures())
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