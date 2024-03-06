using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.IO.Gpx;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// GPS轨迹信息
    /// </summary>
    public class TrackInfo : ICloneable
    {
        public enum TrackSelectionDisplay
        {
            SimpleLine,
            ColoredLine,
        }
        public TrackInfo(string file, Gpx gpx, int trackIndex)
        {
            FilePath = file;
            Gpx = gpx;
            TrackIndex = trackIndex;
            Points = new ObservableCollection<GpxPoint>(Track.GetPoints());
        }

        public TimeSpan TotalTime => Points.GetDuration() ?? TimeSpan.Zero;
        /// <summary>
        /// 轨迹文件名
        /// </summary>
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);

        /// <summary>
        /// 轨迹文件名
        /// </summary>
        public string FilePath { get; init; }

        /// <summary>
        /// 对应的GPX对象
        /// </summary>
        public Gpx Gpx { get; init; }

        /// <summary>
        /// 是否已经平滑
        /// </summary>
        public bool Smoothed { get; set; } = false;

        /// <summary>
        /// GPX轨迹对象
        /// </summary>
        public GpxTrack Track => Gpx.Tracks[TrackIndex];

        public IList<GpxPoint> GetPoints() => Track.GetPoints();

        /// <summary>
        /// 轨迹在GPX中的索引
        /// </summary>
        public int TrackIndex { get; set; }
        public ObservableCollection<GpxPoint> Points { get; }
        /// <summary>
        /// 对应的图形
        /// </summary>
        private GraphicsOverlay ColoredLineOverlay { get; set; }

        /// <summary>
        /// 对应的图形
        /// </summary>
        private GraphicsOverlay SimpleLineOverlay { get; set; }

        public void AddToOverlays(GraphicsOverlayCollection overlays, Dictionary<GraphicsOverlay, TrackInfo> dictionary)
        {
            overlays.Add(SimpleLineOverlay);
            overlays.Add(ColoredLineOverlay);
            dictionary.Add(SimpleLineOverlay, this);
            dictionary.Add(ColoredLineOverlay, this);
        }
        public TrackInfo Clone()
        {
            return MemberwiseClone() as TrackInfo;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public GraphicCollection GetGraphic(TrackSelectionDisplay display)
        {
            GraphicsOverlay overlay = GetOverlay(display);
            return overlay.Graphics;
        }

        public LayerSceneProperties GetSceneProperties(TrackSelectionDisplay display)
        {
            GraphicsOverlay overlay = GetOverlay(display);
            return overlay.SceneProperties;
        }

        public void Initialize()
        {
            SimpleLineOverlay = new GraphicsOverlay()
            {
                Renderer = new SimpleRenderer(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 3)),
                IsVisible = true,
            };
            ColoredLineOverlay = new GraphicsOverlay()
            {
                IsVisible = false,
            };
        }

        public void RemoveFromOverlays(GraphicsOverlayCollection overlays, Dictionary<GraphicsOverlay, TrackInfo> dictionary)
        {
            overlays.Remove(SimpleLineOverlay);
            overlays.Remove(ColoredLineOverlay);
            dictionary.Remove(SimpleLineOverlay);
            dictionary.Remove(ColoredLineOverlay);
        }
        public void SetRenderer(TrackSelectionDisplay display, Renderer renderer)
        {
            GraphicsOverlay overlay = GetOverlay(display);
            overlay.Renderer = renderer;
        }

        public void UpdateTrackDisplay(TrackSelectionDisplay display)
        {
            SimpleLineOverlay.IsVisible = display == TrackSelectionDisplay.SimpleLine;
            ColoredLineOverlay.IsVisible = display == TrackSelectionDisplay.ColoredLine;
        }

        private GraphicsOverlay GetOverlay(TrackSelectionDisplay display)
        {
            return display switch
            {
                TrackSelectionDisplay.SimpleLine => SimpleLineOverlay,
                TrackSelectionDisplay.ColoredLine => ColoredLineOverlay,
                _ => null
            };
        }
    }
}