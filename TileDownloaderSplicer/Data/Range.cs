using FzLib.Geography.IO.Tile;
using GIS.Geometry;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using GeoPoint = NetTopologySuite.Geometries.Point;

namespace MapBoard.TileDownloaderSplicer
{
    public class Range<T>
    {
        public Range()
        {

        }
        public Range(T xMinOrLeft, T xMaxOrRight, T yMinOrBottom, T yMaxOrTop)
        {
            SetValue(xMinOrLeft, xMaxOrRight, yMinOrBottom, yMaxOrTop);
        }
        public T XMax_Right { get; set; }
        public T XMin_Left { get; set; }
        public T YMax_Top { get; set; }
        public T YMin_Bottom { get; set; }
        public void SetValue(T xMinOrLeft, T xMaxOrRight, T yMinOrBottom, T yMaxOrTop)
        {
            XMin_Left = xMinOrLeft;
            XMax_Right = xMaxOrRight;
            YMin_Bottom = yMinOrBottom;
            YMax_Top = yMaxOrTop;
        }
    }
}
