using FzLib.Geography.Coordinate;

namespace MapBoard.TileDownloaderSplicer
{
    public class DownloadInfo
    {
        public GeoPoint LeftUpPoint { get; set; }
        public GeoPoint RightDownPoint { get; set; }
        public int MinLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 10;
    }

}
