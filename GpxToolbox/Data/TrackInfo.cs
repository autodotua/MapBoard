using Esri.ArcGISRuntime.UI;
using FzLib.Geography.IO.Gpx;
using System;
using System.IO;

namespace MapBoard.GpxToolbox
{
    public class TrackInfo : ICloneable
    {
        public string FilePath { get; set; }
        public GraphicsOverlay Overlay { get; set; }
        public Gpx Gpx { get; set; }
        public int TrackIndex { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public GpxTrack Track => Gpx.Tracks[TrackIndex];

        public bool Smoothed { get; set; } = false;

        public TrackInfo Clone()
        {
            return MemberwiseClone() as TrackInfo;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}