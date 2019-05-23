using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using FzLib.Extension;
using GIS.IO.Gpx;
using MapBoard.Common;
using System;
using System.Collections.Generic;
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
using GeoPoint = GIS.Geometry.GeoPoint;

namespace MapBoard.GpxToolbox
{
    public class ArcMapView : SceneView
    {
        public ArcMapView()
        {
            Loaded += ArcMapViewLoaded;
            GeoViewTapped += MapViewTapped;
            AllowDrop = true;
            TrackInfo.Tracks.CollectionChanged += TracksCollectionChanged;

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
                Track = TrackInfo.Tracks;
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
                        var exist = TrackInfo.Tracks.FirstOrDefault(p => p.FilePath == file);
                        if (exist != null)
                        {
                            //GraphicsOverlays.Remove(exist.Overlay);
                            TrackInfo.Tracks.Remove(exist);
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
            GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrack.ToArray()));
        }
        public TwoWayDictionary<GpxPoint, Graphic> gpxPointAndGraphics = new TwoWayDictionary<GpxPoint, Graphic>();

        //public Dictionary<GpxPoint, TrackInfo> pointToTrackInfo = new Dictionary<GpxPoint, TrackInfo>();
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

            //selectedGraphics.Add(track.Overlay.Graphics[0]);
            int index = track.Track.Points.IndexOf(point);
            for (int i = 1; i < index; i++)
            {
                var p = track.Track.Points[i];

                if (gpxPointAndGraphics.ContainsKey(p) )
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

                    //if (selectedGraphics.Contains(g))
                    //{
                    UnselectPoint(g);
                    //}
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
            if(SelectedTrack==null)
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
            //await this.LoadBaseMapsAsync();
            ////var baseLayer = new WebTiledLayer("http://online{num}.map.bdimg.com/tile/?qt=tile&x={col}&y={row}&z={level}&styles=pl&scaler=1&udt=20141103", new string[] { "1", "2", "3", "4" });
            ////baseLayer = new WebTiledLayer(@"files:///C:/Users/autod/OneDrive/同步/作品/瓦片下载拼接器/MapBoard.TileDownloaderSplicer/bin/Debug/Download/{level}/{col}-{row}.png", new string[] {  "2", "3", "4" });
            //// baseLayer = new WebTiledLayer("http://127.0.0.1:8080/{col}-{row}-{level}", new string[] {  "2", "3", "4" });
            //baseLayer = new WebTiledLayer("http://webrd0{subDomain}.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=7&x={col}&y={row}&z={level}", new string[] { "1", "2", "3", "4" });
            //Basemap basemap = new Basemap(baseLayer);
            ////Basemap basemap = new Basemap(new WebTiledLayer("http://webrd0{subDomain}.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=7&x={col}&y={row}&z={level}", new string[] { "1", "2", "3", "4" }));
            //await basemap.LoadAsync();
            //Map map = new Map(basemap);
            //await map.LoadAsync();
            //Map = map;
            var map = this;
            await this.LoadBaseMapsAsync();
            //Surface elevationSurface = new Surface();
            //string _elevationServiceUrl = "http://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";
            //ArcGISTiledElevationSource elevationSource = new ArcGISTiledElevationSource(new Uri(_elevationServiceUrl));
            //elevationSurface.ElevationSources.Add(elevationSource);
            //Scene.BaseSurface = elevationSurface;
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
                }; try
                {
                    LoadTrack(trackInfo);
                    loadedTrack.Add(trackInfo);
                }
                catch (Exception ex)
                {
                    Log.ErrorLogs.Add("加载gpx文件" + filePath + "的Track(" + i.ToString() + ")错误：" + ex.Message);
                }

            }
            if (raiseEvent)
            {
                GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrack.ToArray()));
            }

            return loadedTrack;

        }

        public void LoadTrack(TrackInfo trackInfo, bool update = false)
        {

            if (update)
            {
                //foreach (var point in trackInfo.Overlay.Graphics.Skip(1).Select(p => gpxPointAndGraphics.GetKey(p)).ToArray())
                //{
                //    gpxPointAndGraphics.Remove(point);
                //}
                foreach (var point in trackInfo.Track.Points)
                {if(gpxPointAndGraphics.ContainsKey(point))
                    gpxPointAndGraphics.Remove(point);
                }
                trackInfo.Overlay.Graphics.Clear();
            }
            double minZ = Config.Instance.GpxHeight && Config.Instance.GpxRelativeHeight ? trackInfo.Track.Points.Min(p => p.Z) : 0;
            double mag = Config.Instance.GpxHeight ? Config.Instance.GpxHeightExaggeratedMagnification : 1;
            // List<MapPoint> points = info.Tracks[0].GetOffsetPoints(OffsetNorth, OffsetEast);
            List<ArcMapPoint> mapPoints = new List<ArcMapPoint>();
            foreach (var p in trackInfo.Track.Points)
            {
                //if (update)
                //{
                //    if (!pointToTrackInfo.ContainsKey(p))
                //    {
                //        pointToTrackInfo.Add(p, trackInfo);
                //    }
                //}
                //else
                //{
                //    pointToTrackInfo.Add(p, trackInfo);
                //}
                GeoPoint newP = p;
                if (Config.Instance.BasemapCoordinateSystem != "WGS84")
                {
                    CoordinateTransformation transformation = new CoordinateTransformation("WGS84", Config.Instance.BasemapCoordinateSystem);
                    transformation.TransformateSelf(newP);
                }

                ArcMapPoint point = new ArcMapPoint(newP.X, newP.Y, (p.Z - minZ) * mag, SpatialReferences.Wgs84);
                mapPoints.Add(point);
                Graphic graphic = new Graphic(point);
                //if (update)
                //{
                    if (!gpxPointAndGraphics.ContainsKey(p))
                    {
                        gpxPointAndGraphics.Add(p, graphic);
                    }
                //}
                //else
                //{
                //    gpxPointAndGraphics.Add(p, graphic);
                //}
                trackInfo.Overlay.Graphics.Add(graphic);
            }
            Graphic lineGraphic = new Graphic(new Polyline(mapPoints));
            //SimpleRenderer renderer = new SimpleRenderer(symbol);
            // lineGraphic.Symbol = NormalLineSymbol;
            trackInfo.Overlay.Graphics.Insert(0, lineGraphic);
            if (Config.Instance.GpxHeight)
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
                TrackInfo.Tracks.Add(trackInfo);
            }

        }


        public void StartDraw()
        {
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
            public GpxLoadedEventArgs(TrackInfo[] track)
            {
                Track = track;
            }

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
