using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic.Collection;
using FzLib.Extension;
using FzLib.Geography.Format;
using MapBoard.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static MapBoard.GpxToolbox.SymbolResources;
using ArcMapPoint = Esri.ArcGISRuntime.Geometry.MapPoint;
using MapPoint = FzLib.Geography.Coordinate.GeoPoint;

namespace MapBoard.GpxToolbox
{
    public class ArcMapView : MapView
    {
        public ArcMapView()
        {
            Loaded += ArcMapViewLoaded;
            GeoViewTapped += MapViewTapped;
            AllowDrop = true;
        }
        public bool EnableSelection { get; set; } = true;
        private void MapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!EnableSelection)
            {
                return;
            }
            var clickPoint = ScreenToLocation(e.Position);
            double tolerance = 5;
            double mapTolerance = tolerance * UnitsPerPixel;
            Envelope envelope = new Envelope(clickPoint.X - mapTolerance, clickPoint.Y - mapTolerance, clickPoint.X + mapTolerance, clickPoint.Y + mapTolerance, SpatialReference);
            bool multiple = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);


            ClearSelection();
            var overlay = SelectedTrajectorie.Overlay;

            foreach (var graphic in overlay.Graphics)
            {
                var newPoint = GeometryEngine.Project(graphic.Geometry, SpatialReference);
                if (GeometryEngine.Within(newPoint, envelope))
                {
                    SelectPoint(graphic);
                    PointSelected?.Invoke(this, new PointSelectedEventArgs(mapPointAndGraphics.GetKey(graphic)));
                    return;
                }

            }
        }


        public async void LoadFiles(IEnumerable<string> files)
        {
            List<TrajectoryInfo> loadedTrajectories = new List<TrajectoryInfo>();
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
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
                    var exist = TrajectoryInfo.Trajectories.FirstOrDefault(p => p.FilePath == file);
                    if (exist != null)
                    {
                        GraphicsOverlays.Remove(exist.Overlay);
                        TrajectoryInfo.Trajectories.Remove(exist);
                    }
                    else
                    {
                        loadedTrajectories.AddRange(await LoadGpx(file, false));
                    }
                }
            }
            GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrajectories.ToArray()));

            if (TrajectoryInfo.Trajectories.Count > 0)
            {
                //var overlay = TrajectoryInfo.Trajectories.Last().Overlay;

                //if (overlay.Graphics.Count > 0)
                //{
                //    await SetViewpointGeometryAsync(overlay.Extent);
                //}
            }
        }

        private Dictionary<MapPoint, TrajectoryInfo> pointToTrajectoryInfo = new Dictionary<MapPoint, TrajectoryInfo>();
        public void SelectPoint(MapPoint point)
        {
            SelectPoint(mapPointAndGraphics[point]);
        }
        public void SelectPointTo(MapPoint point)
        {
            bool isOk = false;
            foreach (var p in pointToTrajectoryInfo[point].Gpx.Tracks[pointToTrajectoryInfo[point].TrackIndex].Points)
            {
                if (point == p)
                {
                    isOk = true;
                }
                if (isOk)
                {
                    if (mapPointAndGraphics[p].Symbol != null)
                    {
                        mapPointAndGraphics[p].Symbol = null;
                    }
                }
                else
                {
                    SelectPoint(mapPointAndGraphics[p]);
                }
            }
        }
        public void SelectPoint(Graphic point)
        {

            point.Symbol = GetSelectedSymbol();
            selectedPoints.Add(point);
        }
        public void ClearSelection()
        {
            foreach (var point in selectedPoints)
            {
                point.Symbol = null;
            }
            selectedPoints.Clear();
        }

        private List<Graphic> selectedPoints = new List<Graphic>();

        private async void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            await this.LoadBaseMapsAsync();
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
        }


        public async Task<List<TrajectoryInfo>> LoadGpx(string filePath, bool raiseEvent)
        {
            string gpxContent = File.ReadAllText(filePath);
            GpxInfo info = null;
            await Task.Run(() =>
             {
                 info = GpxInfo.FromString(gpxContent);
             });
            List<TrajectoryInfo> loadedTrajectories = new List<TrajectoryInfo>();
            for (int i = 0; i < info.Tracks.Count; i++)
            {
                var overlay = new GraphicsOverlay() { Renderer = GetNormalOverlayRenderer() };
                var trajectoryInfo = new TrajectoryInfo()
                {
                    FilePath = filePath,
                    Overlay = overlay,
                    TrackIndex = i,
                    Gpx = info,
                };
                try
                {
                    // List<MapPoint> points = info.Tracks[0].GetOffsetPoints(OffsetNorth, OffsetEast);
                    foreach (var item in trajectoryInfo.Track.Points)
                    {
                        pointToTrajectoryInfo.Add(item, trajectoryInfo);
                        var newP = FzLib.Geography.Coordinate.Convert.GeoCoordConverter.WGS84ToGCJ02(new MapPoint(item.Latitude, item.Longitude));

                        ArcMapPoint point = new ArcMapPoint(newP.Longitude, newP.Latitude, SpatialReferences.Wgs84);
                        Graphic graphic = new Graphic(point);
                        mapPointAndGraphics.Add(item, graphic);
                        overlay.Graphics.Add(graphic);
                    }
                    GraphicsOverlays.Add(overlay);
                    TrajectoryInfo.Trajectories.Add(trajectoryInfo);
                    loadedTrajectories.Add(trajectoryInfo);
                }
                catch (Exception ex)
                {
                    Log.ErrorLogs.Add("加载gpx文件" + filePath + "的Track" + i.ToString() + "错误：" + ex.Message);
                }
            }
            if (raiseEvent)
            {
                GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrajectories.ToArray()));
            }

            return loadedTrajectories;

        }
        public TwoWayDictionary<MapPoint, Graphic> mapPointAndGraphics = new TwoWayDictionary<MapPoint, Graphic>();
        private TrajectoryInfo selectedTrajectorie = null;

        public TrajectoryInfo SelectedTrajectorie
        {
            get => selectedTrajectorie;
            set
            {
                selectedTrajectorie = value;
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
            public PointSelectedEventArgs(MapPoint point)
            {
                Point = point;
            }

            public MapPoint Point { get; private set; }
        }
        public delegate void PointSelectedEventHandler(object sender, PointSelectedEventArgs e);
        public event PointSelectedEventHandler PointSelected;

        public class GpxLoadedEventArgs : EventArgs
        {
            public GpxLoadedEventArgs(TrajectoryInfo[] trajectories)
            {
                Trajectories = trajectories;
            }

            public TrajectoryInfo[] Trajectories { get; private set; }
        }
        public delegate void GpxLoadedEventHandler(object sender, GpxLoadedEventArgs e);
        public event GpxLoadedEventHandler GpxLoaded;
    }

}
