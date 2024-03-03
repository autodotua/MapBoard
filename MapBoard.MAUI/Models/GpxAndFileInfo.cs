using MapBoard.IO.Gpx;
using MapBoard.Models;

namespace MapBoard.Models
{
    public class GpxAndFileInfo
    {
        public GpxAndFileInfo(string file)
        {
            File = new SimpleFile(file);
            Gpx=Gpx.LoadMetadatasFromFile(file);
        }
        public SimpleFile File { get; set; }
        public Gpx Gpx { get; set; }
    }
}
