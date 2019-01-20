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
        public static void Import(string path)
        {
            string[] files = GetExistShapefiles(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)).ToArray();
            string[] existFiles = Directory.EnumerateFiles(Config.DataPath).Select(p => Path.GetFileName(p)).ToArray();

            foreach (var file in files)
            {
                if (existFiles.Contains(Path.GetFileName(file)))
                {
                    throw new Exception("文件名" + Path.GetFileName(file) + "与现有文件冲突");
                }
            }

            foreach (var file in files)
            {
                File.Move(file, Path.Combine(Config.DataPath, Path.GetFileName(file)));
            }
            StyleHelper.AddStyle(Path.GetFileNameWithoutExtension(path));
        }
    }
}
