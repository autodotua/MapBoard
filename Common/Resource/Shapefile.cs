using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using MapBoard.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapBoard.Common.Resource
{
    /// <summary>
    /// 空shapefile导出类
    /// </summary>
    public static class ShapefileExport
    {
        public static string ExportEmptyPointShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string path = GetFileName(folderPath, name, "shp");
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.PointCpg);
            File.WriteAllBytes(path, Resource.PointShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.PointShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.PointPrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.PointDbf);
            return path;
        }
        public static string ExportEmptyMultipointShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string path = GetFileName(folderPath, name, "shp");
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.MultipointCpg);
            File.WriteAllBytes(path, Resource.MultipointShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.MultipointShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.MultipointPrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.MultipointDbf);
            return path;
        }
        public static string ExportEmptyPolylineShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string path = GetFileName(folderPath, name, "shp");
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.PolylineCpg);
            File.WriteAllBytes(path, Resource.PolylineShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.PolylineShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.PolylinePrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.PolylineDbf);
            return path;
        }
        public static string ExportEmptyPolygonShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string path = GetFileName(folderPath, name, "shp");
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.PolygonCpg);
            File.WriteAllBytes(path, Resource.PolygonShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.PolygonShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.PolygonPrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.PolygonDbf);
            return path;
        }
        private static string GetFileName(string folderPath, string name, string extension)
        {
            if (folderPath.EndsWith("\\"))
            {
                folderPath = folderPath.RemoveEnd("\\", true);
            }
            if (extension.StartsWith("."))
            {
                extension = extension.RemoveStart(".", true);
            }
            if (name.Contains("."))
            {
                name = Path.GetFileNameWithoutExtension(name);
            }
            return folderPath + "\\" + name + "." + extension;
        }

        public static string ExportEmptyShapefile(GeometryType type, string name, string path = null)
        {
            if (path == null)
            {
                path = Config.DataPath;
            }
            switch (type)
            {
                case GeometryType.Point:
                    return ExportEmptyPointShapefile(path, name);
                case GeometryType.Multipoint:
                    return ExportEmptyMultipointShapefile(path, name);
                case GeometryType.Polyline:
                    return ExportEmptyPolylineShapefile(path, name);
                case GeometryType.Polygon:
                    return ExportEmptyPolygonShapefile(path, name);
                default:
                    throw new Exception("不支持的格式");
            }
        }
    }
}