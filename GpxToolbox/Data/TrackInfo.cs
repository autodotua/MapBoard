using Esri.ArcGISRuntime.UI;
using FzLib.Geography.IO.Gpx;
using System.Collections.ObjectModel;
using System.IO;

namespace MapBoard.GpxToolbox
{
    public class TrackInfo
    {
        public static ObservableCollection<TrackInfo> Tracks { get; } = new ObservableCollection<TrackInfo>();
  
        public string FilePath { get; set; }
        public GraphicsOverlay Overlay { get; set; }
        public Gpx Gpx { get; set; }
        public int TrackIndex { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public GpxTrack Track => Gpx.Tracks[TrackIndex];

        public bool Smoothed { get; set; } = false;
    }
}
