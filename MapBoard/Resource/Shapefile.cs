using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using MapBoard.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapBoard.Resource
{
    public static class Shapefile
    {
        public static void ExportEmptyPointShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.PointCpg);
            File.WriteAllBytes(GetFileName(folderPath, name, "shp"), Resource.PointShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.PointShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.PointPrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.PointDbf);
        }
        public static void ExportEmptyMultipointShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.MultipointCpg);
            File.WriteAllBytes(GetFileName(folderPath, name, "shp"), Resource.MultipointShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.MultipointShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.MultipointPrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.MultipointDbf);
        }
        public static void ExportEmptyPolylineShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.PolylineCpg);
            File.WriteAllBytes(GetFileName(folderPath, name, "shp"), Resource.PolylineShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.PolylineShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.PolylinePrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.PolylineDbf);
        }
        public static void ExportEmptyPolygonShapefile(string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            File.WriteAllBytes(GetFileName(folderPath, name, "cpg"), Resource.PolygonCpg);
            File.WriteAllBytes(GetFileName(folderPath, name, "shp"), Resource.PolygonShp);
            File.WriteAllBytes(GetFileName(folderPath, name, "shx"), Resource.PolygonShx);
            File.WriteAllBytes(GetFileName(folderPath, name, "prj"), Resource.PolygonPrj);
            File.WriteAllBytes(GetFileName(folderPath, name, "dbf"), Resource.PolygonDbf);
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

        public static void ExportEmptyShapefile(GeometryType type, string name)
        {
            switch (type)
            {
                case GeometryType.Point:
                    Shapefile.ExportEmptyPointShapefile(Config.DataPath, name);
                    break;
                case GeometryType.Multipoint:
                    Shapefile.ExportEmptyMultipointShapefile(Config.DataPath, name);
                    break;
                case GeometryType.Polyline:
                    Shapefile.ExportEmptyPolylineShapefile(Config.DataPath, name);
                    break;
                case GeometryType.Polygon:
                    Shapefile.ExportEmptyPolygonShapefile(Config.DataPath, name);
                    break;
                default:
                    throw new Exception("不支持的格式");
            }
        }
    }
}