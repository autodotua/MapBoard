using Esri.ArcGISRuntime.Data;
using FzLib.Basic;
using FzLib.Control.Dialog;
using MapBoard.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public async static Task Import(string path)
        {
            //string[] files = GetExistShapefiles(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)).ToArray();
            //string[] existFiles = Directory.EnumerateFiles(Config.DataPath).Select(p => Path.GetFileName(p)).ToArray();

            //foreach (var file in files)
            //{
            //    if (existFiles.Contains(Path.GetFileName(file)))
            //    {
            //        throw new Exception("文件名" + Path.GetFileName(file) + "与现有文件冲突");
            //    }
            //}

            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            await table.LoadAsync();
            bool info = table.Fields.Any(p => p.Name == Resource.Resource.DisplayFieldName && p.FieldType == FieldType.Text);
            bool date = table.Fields.Any(p => p.Name == Resource.Resource.TimeExtentFieldName && p.FieldType == FieldType.Date);
            FeatureQueryResult features = await table.QueryFeaturesAsync(new QueryParameters());

            StyleInfo style = StyleHelper.CreateStyle(table.GeometryType);
            foreach (var feature in features)
            {
                Feature newFeature = style.Table.CreateFeature();
                newFeature.Geometry = feature.Geometry;
                if (info)
                {
                    newFeature.Attributes[Resource.Resource.DisplayFieldName] = feature.Attributes[Resource.Resource.DisplayFieldName];
                }
                if (date)
                {
                    newFeature.Attributes[Resource.Resource.TimeExtentFieldName] = feature.Attributes[Resource.Resource.TimeExtentFieldName];
                }

                await style.Table.AddFeatureAsync(newFeature);
            }
            style.UpdateFeatureCount();

            //foreach (var file in files)
            //{
            //    File.Move(file, Path.Combine(Config.DataPath, Path.GetFileName(file)));
            //}
            //StyleHelper.AddStyle(Path.GetFileNameWithoutExtension(path));
        }
    }
}
