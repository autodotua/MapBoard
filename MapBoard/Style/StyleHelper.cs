using Esri.ArcGISRuntime.Geometry;
using FzLib.Geography.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Style
{
    public static class StyleHelper
    {
        public static void RemoveStyle(StyleInfo style, bool deleteFiles)
        {
            if (StyleCollection.Instance.Styles.Contains(style))
            {
                StyleCollection.Instance.Styles.Remove(style);
            }


            if (deleteFiles)
            {
                foreach (var file in Directory.EnumerateFiles(Config.DataPath))
                {
                    if (Path.GetFileNameWithoutExtension(file) == style.Name)
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        public static StyleInfo CreateStyle(GeometryType type)
        {

            string fileName = "新样式-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");

            switch (type)
            {
                case GeometryType.Point:
                    Shapefile.ExportEmptyPointShapefile(Config.DataPath, fileName);
                    break;
                case GeometryType.Multipoint:
                    Shapefile.ExportEmptyMultipointShapefile(Config.DataPath, fileName);
                    break;
                case GeometryType.Polyline:
                    Shapefile.ExportEmptyPolylineShapefile(Config.DataPath, fileName);
                    break;
                case GeometryType.Polygon:
                    Shapefile.ExportEmptyPolygonShapefile(Config.DataPath, fileName);
                    break;
                default:
                    throw new Exception("不支持的格式");
            }
            StyleInfo style = new StyleInfo();
            style.Name = fileName;
            StyleCollection.Instance.Styles.Add(style);
            StyleCollection.Instance.Selected = style;
            return style;
        }


    }
}
