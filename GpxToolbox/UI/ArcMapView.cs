using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic.Collection;
using FzLib.Geography.IO.Gpx;
using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.Common.BaseLayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static MapBoard.GpxToolbox.SymbolResources;
using ArcMapPoint = Esri.ArcGISRuntime.Geometry.MapPoint;
using GeoPoint = NetTopologySuite.Geometries.Point;

namespace MapBoard.GpxToolbox
{
    public class ArcMapView : SceneView
    {
        public ObservableCollection<TrackInfo> Tracks { get; set; }

        public ArcMapView()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new Exception("不允许多实例");
            }
            Loaded += ArcMapViewLoaded;
            GeoViewTapped += MapViewTapped;
            AllowDrop = true;
            SetHideWatermark();
        }

        public static ArcMapView Instance { get; private set; }

        public void SetHideWatermark()
        {
            Margin = new Thickness(Config.Instance.HideWatermark ? -72 : 0);
        }

        private void TracksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    foreach (TrackInfo item in e.OldItems)
                    {
                        //GraphicsOverlays.Remove(item.LineOverlay);
                        foreach (var point in item.Track.Points)
                        {
                            gpxPointAndGraphics.Remove(point);
                        }
                        GraphicsOverlays.Remove(item.Overlay);
                        //foreach (var point in item.Overlay.Graphics.Select(p=>gpxPointAndGraphics.GetKey(p)).ToArray())
                        //{
                        //    gpxPointAndGraphics.Remove(point);
                        //}
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    GraphicsOverlays.Clear();
                    gpxPointAndGraphics.Clear();
                    //pointToTrackInfo.Clear();
                    break;
            }
        }

        public GraphicsOverlay TapOverlay { get; set; }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] files) || files.Length == 0)
            {
                return;
            }
            bool yes = true;
            if (files.Length > 10)
            {
                yes = TaskDialog.ShowWithYesNoButtons("导入文件较多，是否确定导入？", $"导入{files.Length}个文件") == true;
            }
            if (yes)
            {
                LoadFiles(files);
            }
        }

        public MapTapModes MapTapMode { get; set; } = MapTapModes.None;

        private async void MapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (MapTapMode != MapTapModes.AllLayers && MapTapMode != MapTapModes.SelectedLayer)
            {
                return;
            }
            PointSelecting?.Invoke(this, new EventArgs());
            var clickPoint = await ScreenToLocationAsync(e.Position);
            double tolerance = 1 * Camera.Location.Z * 1e-7;
            double mapTolerance = tolerance;
            Envelope envelope = new Envelope(clickPoint.X - mapTolerance, clickPoint.Y - mapTolerance, clickPoint.X + mapTolerance, clickPoint.Y + mapTolerance, SpatialReference);
            bool multiple = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            ClearSelection();
            IEnumerable<TrackInfo> Track = null;
            if (MapTapMode == MapTapModes.SelectedLayer)
            {
                Track = new TrackInfo[] { SelectedTrack };
            }
            else
            {
                Track = Tracks;
            }
            foreach (var trajectory in Track)
            {
                var overlay = trajectory.Overlay;

                foreach (var graphic in overlay.Graphics)
                {
                    var newPoint = GeometryEngine.Project(graphic.Geometry, SpatialReference);
                    if (GeometryEngine.Within(newPoint, envelope))
                    {
                        //SelectPoint(graphic);
                        PointSelected?.Invoke(this, new PointSelectedEventArgs(trajectory, gpxPointAndGraphics.GetKey(graphic)));
                        return;
                    }
                }
            }

            PointSelected?.Invoke(this, new PointSelectedEventArgs(null, null));

            SnakeBar.Show("没有识别到任何" + (MapTapMode == MapTapModes.SelectedLayer ? "点" : "轨迹"));
        }

        public async void LoadFiles(IEnumerable<string> files)
        {
            List<TrackInfo> loadedTrack = new List<TrackInfo>();
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                try
                {
                    if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        Log.ErrorLogs.Add(file + "是目录不是文件");
                    }
                    else if (!fileInfo.Exists)
                    {
                        Log.ErrorLogs.Add(file + "不存在");
                    }
                    else if (fileInfo.Length > 10 * 1024 * 1024)
                    {
                        Log.ErrorLogs.Add("gpx文件" + file + "大于1MB，跳过");
                    }
                    else if (fileInfo.Extension != ".gpx")
                    {
                        Log.ErrorLogs.Add("文件" + file + "不是gpx");
                        continue;
                    }
                    else
                    {
                        var exist = Tracks.FirstOrDefault(p => p.FilePath == file);
                        if (exist != null)
                        {
                            Tracks.Remove(exist);
                        }
                        else
                        {
                            loadedTrack.AddRange(await LoadGpx(file, false));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorLogs.Add(ex.Message);
                }
            }
            GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrack.ToArray(), false));
        }

        public TwoWayDictionary<GpxPoint, Graphic> gpxPointAndGraphics = new TwoWayDictionary<GpxPoint, Graphic>();

        public void SelectPoints(IEnumerable<GpxPoint> points)
        {
            var graphics = SelectedTrack.Overlay.Graphics;
            graphics[0].Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Null, Color.Red, 0); ;
            int count = graphics.Count;
            for (int i = 1; i < count; i++)
            {
                var graphic = graphics[i];
                if (points.Contains(gpxPointAndGraphics.GetKey(graphic)))
                {
                    SelectPoint(graphic);
                }
                else
                {
                    UnselectPoint(graphic);
                }
            }
        }

        public void SelectPoint(GpxPoint point)
        {
            SelectPoint(gpxPointAndGraphics[point]);
        }

        public void SelectPointTo(GpxPoint point)
        {
            if (point == null)
            {
                ClearSelection();
                return;
            }
            TrackInfo track = SelectedTrack;
            track.Overlay.Graphics[0].Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Null, Color.Red, 0); ;
            if (track.Overlay.Renderer == null)
            {
                track.Overlay.Renderer = SelectionRenderer;
            }

            int index = track.Track.Points.IndexOf(point);
            for (int i = 1; i < index; i++)
            {
                var p = track.Track.Points[i];

                if (gpxPointAndGraphics.ContainsKey(p))
                {
                    Graphic g = gpxPointAndGraphics[p];
                    if (!selectedGraphics.Contains(g))
                    {
                        SelectPoint(g);
                    }
                }
            }
            var count = track.Track.Points.Count;

            for (int i = index; i < count; i++)
            {
                var p = track.Track.Points[i];
                if (gpxPointAndGraphics.ContainsKey(p))
                {
                    Graphic g = gpxPointAndGraphics[p];

                    UnselectPoint(g);
                }
            }
        }

        private void SelectPoint(Graphic g)
        {
            g.Symbol = SelectedPointSymbol;
            selectedGraphics.Add(g);
        }

        public void UnselectPoint(Graphic g)
        {
            g.Symbol = NotSelectedPointSymbol;
            selectedGraphics.Remove(g);
        }

        public void ClearSelection()
        {
            if (SelectedTrack == null)
            {
                return;
            }
            SelectedTrack.Overlay.Renderer = CurrentRenderer;
            foreach (var g in SelectedTrack.Overlay.Graphics)
            {
                g.Symbol = null;
            }
            SelectedTrack.Overlay.Graphics[0].Symbol = null;
            selectedGraphics.Clear();
        }

        private HashSet<Graphic> selectedGraphics = new HashSet<Graphic>();

        private async void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            if (Tracks != null)
            {
                Tracks.CollectionChanged += TracksCollectionChanged;
            }
            await GeoViewHelper.LoadBaseGeoViewAsync(this);
        }

        private GraphicsOverlay browseOverlay;

        public void SetLocation(GeoPoint p)
        {
            if (browseOverlay == null)
            {
                browseOverlay = new GraphicsOverlay();
                GraphicsOverlays.Add(browseOverlay);
                browseOverlay.Renderer = BrowsePointRenderer;
            }
            var point = new ArcMapPoint(p.X, p.Y, p.Z);
            if (browseOverlay.Graphics.Count == 0)
            {
                browseOverlay.Graphics.Add(new Graphic() { Geometry = point });
            }
            else
            {
                browseOverlay.Graphics[0].Geometry = point;
            }
            //EventHandler<DrawStatusChangedEventArgs> handler = null;
            //handler= new EventHandler<DrawStatusChangedEventArgs>((s, e) => {
            //    DrawStatusChanged -= handler;

            //});
            //DrawStatusChanged += handler;
        }

        public async Task<List<TrackInfo>> ReloadGpx(TrackInfo track, bool raiseEvent)
        {
            Tracks.Remove(track);
            var ts = await LoadGpx(track.FilePath, false);
            if (raiseEvent)
            {
                GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(ts.ToArray(), true));
            }
            return ts;
        }

        public async Task<List<TrackInfo>> LoadGpx(string filePath, bool raiseEvent)
        {
            string gpxContent = File.ReadAllText(filePath);
            Gpx gpx = null;
            await Task.Run(() =>
             {
                 gpx = Gpx.FromString(gpxContent);
             });
            List<TrackInfo> loadedTrack = new List<TrackInfo>();
            for (int i = 0; i < gpx.Tracks.Count; i++)
            {
                var overlay = new GraphicsOverlay() { Renderer = NormalRenderer };
                var trackInfo = new TrackInfo()
                {
                    FilePath = filePath,
                    Overlay = overlay,
                    TrackIndex = i,
                    Gpx = gpx,
                };
                try
                {
                    LoadTrack(trackInfo, false, true);
                    loadedTrack.Add(trackInfo);
                }
                catch (Exception ex)
                {
                    Log.ErrorLogs.Add("加载gpx文件" + filePath + "的Track(" + i.ToString() + ")错误：" + ex.Message);
                }
            }
            if (raiseEvent)
            {
                GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrack.ToArray(), false));
            }

            return loadedTrack;
        }

        public void LoadTrack(TrackInfo trackInfo, bool update = false, bool raiseEvent = false, bool? gpxHeight = null)
        {
            if (update)
            {
                foreach (var point in trackInfo.Track.Points)
                {
                    if (gpxPointAndGraphics.ContainsKey(point))
                        gpxPointAndGraphics.Remove(point);
                }
                trackInfo.Overlay.Graphics.Clear();
            }
            if (Config.Instance.GpxAutoSmooth)
            {
                GpxHelper.Smooth(trackInfo.Track.Points, Config.Instance.GpxAutoSmoothLevel, p => p.Z, (p, v) => p.Z = v);
                if (!Config.Instance.GpxAutoSmoothOnlyZ)
                {
                    GpxHelper.Smooth(trackInfo.Track.Points, Config.Instance.GpxAutoSmoothLevel, p => p.X, (p, v) => p.X = v);
                    GpxHelper.Smooth(trackInfo.Track.Points, Config.Instance.GpxAutoSmoothLevel, p => p.Y, (p, v) => p.Y = v);
                }
                trackInfo.Smoothed = true;
            }
            double minZ = Config.Instance.GpxHeight && Config.Instance.GpxRelativeHeight ? trackInfo.Track.Points.Min(p => p.Z) : 0;
            double mag = Config.Instance.GpxHeight ? Config.Instance.GpxHeightExaggeratedMagnification : 1;
            // List<MapPoint> points = info.Tracks[0].GetOffsetPoints(OffsetNorth, OffsetEast);
            List<ArcMapPoint> mapPoints = new List<ArcMapPoint>();
            foreach (var p in trackInfo.Track.Points)
            {
                GeoPoint newP = p;
                if (Config.Instance.BasemapCoordinateSystem != "WGS84")
                {
                    CoordinateTransformation transformation = new CoordinateTransformation("WGS84", Config.Instance.BasemapCoordinateSystem);
                    transformation.TransformateSelf(newP);
                }

                ArcMapPoint point = new ArcMapPoint(newP.X, newP.Y, (p.Z - minZ) * mag, SpatialReferences.Wgs84);
                mapPoints.Add(point);
                Graphic graphic = new Graphic(point);
                if (!gpxPointAndGraphics.ContainsKey(p))
                {
                    gpxPointAndGraphics.Add(p, graphic);
                }

                trackInfo.Overlay.Graphics.Add(graphic);
            }
            Graphic lineGraphic = new Graphic(new Polyline(mapPoints));
            trackInfo.Overlay.Graphics.Insert(0, lineGraphic);
            if (gpxHeight == true || (!gpxHeight.HasValue) && Config.Instance.GpxHeight)
            {
                trackInfo.Overlay.SceneProperties.SurfacePlacement = SurfacePlacement.Absolute;
            }
            else
            {
                trackInfo.Overlay.SceneProperties.SurfacePlacement = SurfacePlacement.Draped;
            }
            if (!update)
            {
                GraphicsOverlays.Add(trackInfo.Overlay);
                Tracks?.Add(trackInfo);
            }
            if (raiseEvent)
            {
                GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(new TrackInfo[] { trackInfo }, false));
            }
        }

        private TrackInfo selectedTrack = null;

        public TrackInfo SelectedTrack
        {
            get => selectedTrack;
            set
            {
                selectedTrack = value;
                if (value != null && value.Overlay != GraphicsOverlays.Last())
                {
                    GraphicsOverlays.Remove(value.Overlay);
                    GraphicsOverlays.Add(value.Overlay);
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedTrajectorie.Gpx.Name"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public class PointSelectedEventArgs : EventArgs
        {
            public PointSelectedEventArgs(TrackInfo trajectory, GpxPoint point)
            {
                Trajectory = trajectory;
                Point = point;
            }

            public TrackInfo Trajectory { get; private set; }
            public GpxPoint Point { get; private set; }
        }

        public delegate void PointSelectedEventHandler(object sender, PointSelectedEventArgs e);

        public event PointSelectedEventHandler PointSelected;

        public event EventHandler PointSelecting;

        public class GpxLoadedEventArgs : EventArgs
        {
            public GpxLoadedEventArgs(TrackInfo[] track, bool update)
            {
                Track = track;
                Update = update;
            }

            public bool Update { get; private set; }
            public TrackInfo[] Track { get; private set; }
        }

        public delegate void GpxLoadedEventHandler(object sender, GpxLoadedEventArgs e);

        public event GpxLoadedEventHandler GpxLoaded;

        public enum MapTapModes
        {
            None,
            SelectedLayer,
            AllLayers,
            Circle,
        }
    }
}