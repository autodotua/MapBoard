using FzLib.Geography.Coordinate.Convert;

namespace MapBoard.TileDownloaderSplicer
{
    class ProjectInfo
    {
        public ProjectInfo(int level, int x, int y, int width, int height)
        {
            (double lat, double lng) leftTop = TileConverter.PixelToGeoPoint(0, 0, x, y, level);
            (double lat, double lng) right = TileConverter.PixelToGeoPoint(0, 0, x + 1, y, level);
            (double lat, double lng) bottom = TileConverter.PixelToGeoPoint(0, 0, x, y + 1, level);
            PixelPerX = (right.lng - leftTop.lng) * 1.0 / Config.Instance.TileSize.width;
            PixelPerY = (bottom.lat - leftTop.lat) * 1.0 / Config.Instance.TileSize.height;
            NorthwestLatitude = leftTop.lat;
            NorthwestLongitude = leftTop.lng;
        }
        public double PixelPerX { get; set; }
        public double PixelPerY { get; set; }
        public double NorthwestLatitude { get; set; }
        public double NorthwestLongitude { get; set; }

        public override string ToString()
        {
            return $@"{PixelPerX.ToString()}
0
0
{PixelPerY.ToString()}
{NorthwestLongitude.ToString()}
{NorthwestLatitude.ToString()}";
        }
    }

}
