
using GIS.Geometry;
using NetTopologySuite.Geometries;

namespace MapBoard.TileDownloaderSplicer
{
    public class DownloadInfo
    {
        public Point LeftUpPoint { get; set; }
        public Point RightDownPoint { get; set; }
        public int MinLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 10;
    }

}
