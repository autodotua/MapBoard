using Esri.ArcGISRuntime.UI;
using FzLib.Geography.Format;
using System.Collections.ObjectModel;
using System.IO;

namespace MapBoard.GpxToolbox
{
    public class TrajectoryInfo
    {
     
        public static ObservableCollection<TrajectoryInfo> Trajectories { get; } = new ObservableCollection<TrajectoryInfo>();
  
        public string FilePath { get; set; }
        public GraphicsOverlay Overlay { get; set; }
        public GpxInfo Gpx { get; set; }
        public int TrackIndex { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public GpxTrackInfo Track => Gpx.Tracks[TrackIndex];
    }
}
