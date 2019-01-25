using Esri.ArcGISRuntime.Geometry;
using FzLib.Geography.Coordinate;
using FzLib.Geography.Coordinate.Convert;

namespace MapBoard.TileDownloaderSplicer
{
    public class DownloadFileInfo : FzLib.Extension.ExtendedINotifyPropertyChanged
    {
        public DownloadFileInfo(int x, int y, int level)
        {
            X = x;
            Y = y;
            Level = level;
            (double lat, double lng) = TileConverter.PixelToGeoPoint(0, 0, x, y, level);
            WestNorth = new GeoPoint(lat, lng);
           // var prj = GeometryEngine.Project(new MapPoint(lat, lng, SpatialReferences.Wgs84), SpatialReferences.WebMercator) as MapPoint;
            ( lat,  lng) = TileConverter.PixelToGeoPoint(Config.Instance.TileSize.width, Config.Instance.TileSize.height, x, y, level);
            EastSouth = new GeoPoint(lat, lng);

           // PrjX = prj.X;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Level { get; set; }
        //public double PrjX { get; private set; }
        private string status = "就绪";
        public string Status
        {
            get => status;
            set
            {
                status = value;
                Notify(nameof(Status));
            }
        }

        public GeoPoint WestNorth { get;private set; }
        public GeoPoint EastSouth { get; private set; }
    }

}
