
using GIS.Geometry;
using NetTopologySuite.Geometries;

namespace MapBoard.TileDownloaderSplicer
{
    public class DownloadInfo
    {
        public double XMax { get; set; }
        public double XMin { get; set; }
        public double YMax { get; set; }
        public double YMin { get; set; }
        public int MinLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 10;

        public void SetValue(double xMin, double xMax, double yMin,double yMax)
        {
            XMin = XMin;
            XMax = xMax;
            YMin = yMin;
            YMax = YMax;
        }
    }

}
