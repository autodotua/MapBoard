using Esri.ArcGISRuntime.UI;
using GIS.IO.Gpx;
using System.Collections.ObjectModel;
using System.IO;

namespace MapBoard.GpxToolbox
{
    public class TrackInfo
    {
     
        public static ObservableCollection<TrackInfo> Tracks { get; } = new ObservableCollection<TrackInfo>();
  
        public string FilePath { get; set; }
        public GraphicsOverlay Overlay { get; set; }
        public GIS.IO.Gpx.Gpx Gpx { get; set; }
        public int TrackIndex { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public GpxTrack Track => Gpx.Tracks[TrackIndex];


    }
}
