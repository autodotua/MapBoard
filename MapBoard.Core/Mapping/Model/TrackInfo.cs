using Esri.ArcGISRuntime.UI;
using MapBoard.IO.Gpx;
using System;
using System.IO;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// GPS轨迹信息
    /// </summary>
    public class TrackInfo : ICloneable
    {
        /// <summary>
        /// 轨迹文件名
        /// </summary>
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);

        /// <summary>
        /// 轨迹文件名
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 对应的GPX对象
        /// </summary>
        public Gpx Gpx { get; set; }

        /// <summary>
        /// 对应的图形
        /// </summary>
        public GraphicsOverlay Overlay { get; set; }
        /// <summary>
        /// 是否已经平滑
        /// </summary>
        public bool Smoothed { get; set; } = false;

        /// <summary>
        /// GPX轨迹对象
        /// </summary>
        public GpxTrack Track => Gpx.Tracks[TrackIndex];

        /// <summary>
        /// 轨迹在GPX中的索引
        /// </summary>
        public int TrackIndex { get; set; }
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