using MapBoard.IO.Gpx;
using MapBoard.Models;

namespace MapBoard.Models
{
    public class GpxAndFileInfo
    {
        private GpxAndFileInfo()
        {
        }

        public static async Task<GpxAndFileInfo> FromFileAsync(string file)
        {
            return new GpxAndFileInfo
            {
                File = new SimpleFile(file),
                Gpx = await GpxSerializer.LoadMetadatasFromFileAsync(file)
            };
        }

        public SimpleFile File { get; private set; }
        public Gpx Gpx { get; private set; }
    }
}
