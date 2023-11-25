using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.IO.Gpx;
using System;
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
            Point
        }

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

        /// <summary>
        /// 对应的图形
        /// </summary>
        private GraphicsOverlay ColoredLineOverlay { get; set; }

        /// <summary>
        /// 对应的图形
        /// </summary>
        private GraphicsOverlay PointOverlay { get; set; }

        /// <summary>
        /// 对应的图形
        /// </summary>
        private GraphicsOverlay SimpleLineOverlay { get; set; }

        public void AddToOverlays(GraphicsOverlayCollection overlays)
        {
            overlays.Add(SimpleLineOverlay);
            overlays.Add(ColoredLineOverlay);
            overlays.Add(PointOverlay);
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
            PointOverlay = new GraphicsOverlay()
            {
                Renderer = new SimpleRenderer(new SimpleMarkerSymbol()
                {
                    Color = Color.Blue,
                    Size = 3,
                    Style = SimpleMarkerSymbolStyle.Circle,
                }),
                IsVisible = false
            };
        }

        public void RemoveFromOverlays(GraphicsOverlayCollection overlays)
        {
            overlays.Remove(SimpleLineOverlay);
            overlays.Remove(ColoredLineOverlay);
            overlays.Remove(PointOverlay);
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
            PointOverlay.IsVisible = display == TrackSelectionDisplay.Point;
        }

        private GraphicsOverlay GetOverlay(TrackSelectionDisplay display)
        {
            return display switch
            {
                TrackSelectionDisplay.SimpleLine => SimpleLineOverlay,
                TrackSelectionDisplay.ColoredLine => ColoredLineOverlay,
                TrackSelectionDisplay.Point => PointOverlay,
                _ => null
            };
        }
    }
}