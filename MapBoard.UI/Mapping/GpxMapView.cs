using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using MapBoard.IO.Gpx;
using FzLib.WPF.Dialog;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
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
using static MapBoard.UI.GpxToolbox.GpxSymbolResources;
using MapBoard.Model;
using MapBoard.Mapping.Model;
using FzLib.Collection;
using PropertyChanged;
using System.Windows.Controls;
using Point = System.Windows.Point;

namespace MapBoard.Mapping
{
    /// <summary>
    /// GPX地图
    /// </summary>
    [DoNotNotify]
    public class GpxMapView : SceneView
    {
        public TwoWayDictionary<GpxPoint, Graphic> gpxPointAndGraphics = new TwoWayDictionary<GpxPoint, Graphic>();

        /// <summary>
        /// 所有的<see cref="GpxMapView"/>对象
        /// </summary>
        private static List<GpxMapView> instances = new List<GpxMapView>();

        /// <summary>
        /// 游览中的覆盖层
        /// </summary>
        private GraphicsOverlay browseOverlay;

        /// <summary>
        /// 鼠标右键按下位置
        /// </summary>
        private Point mouseRightDownPosition;

        /// <summary>
        /// 选中的图形
        /// </summary>
        private HashSet<Graphic> selectedGraphics = new HashSet<Graphic>();

        /// <summary>
        /// 当前轨迹
        /// </summary>
        private TrackInfo selectedTrack = null;

        public GpxMapView()
        {
            instances.Add(this);
            Loaded += ArcMapViewLoaded;
            GeoViewTapped += MapViewTapped;
            this.SetHideWatermark();
        }

        /// <summary>
        /// 一个GPX轨迹加载完成
        /// </summary>
        public event EventHandler<GpxLoadedEventArgs> GpxLoaded;

        /// <summary>
        /// 一个GPX点被点击
        /// </summary>
        public event EventHandler<PointSelectedEventArgs> PointSelected;

        /// <summary>
        /// 一个点被选中
        /// </summary>
        public event EventHandler PointSelecting;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 地图点击模式
        /// </summary>
        public enum MapTapModes
        {
            /// <summary>
            /// 就绪
            /// </summary>
            None,

            /// <summary>
            /// 选择选中的轨迹中的点
            /// </summary>
            SelectedLayer,

            /// <summary>
            /// 选择轨迹
            /// </summary>
            AllLayers,

            /// <summary>
            /// 框选
            /// </summary>
            Circle,
        }

        /// <summary>
        /// 所有的<see cref="GpxMapView"/>对象
        /// </summary>
        public static IReadOnlyList<GpxMapView> Instances => instances.AsReadOnly();

        /// <summary>
        /// 当前地图点击模式
        /// </summary>
        public MapTapModes MapTapMode { get; set; } = MapTapModes.None;

        /// <summary>
        /// 当前选择的轨迹
        /// </summary>
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

        /// <summary>
        /// 点击地图后显示点击位置的覆盖层
        /// </summary>
        public GraphicsOverlay TapOverlay { get; set; }

        /// <summary>
        /// 加载的轨迹
        /// </summary>
        public ObservableCollection<TrackInfo> Tracks { get; set; }

        /// <summary>
        /// 清空轨迹选择
        /// </summary>
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

        /// <summary>
        /// 加载指定文件的GPX文件
        /// </summary>
        /// <param name="files"></param>
        public async Task LoadFilesAsync(IEnumerable<string> files)
        {
            List<TrackInfo> loadedTrack = new List<TrackInfo>();
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                try
                {
                    if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        App.Log.Warn(file + "是目录不是文件");
                    }
                    else if (!fileInfo.Exists)
                    {
                        App.Log.Warn(file + "不存在");
                    }
                    else if (fileInfo.Length > 10 * 1024 * 1024)
                    {
                        App.Log.Warn("gpx文件" + file + "大于1MB，跳过");
                    }
                    else if (fileInfo.Extension != ".gpx")
                    {
                        App.Log.Warn("文件" + file + "不是gpx");
                        continue;
                    }
                    else
                    {
                        var exist = Tracks.FirstOrDefault(p => p.FilePath == file);
                        if (exist == null)

                        {
                            loadedTrack.AddRange(await LoadGpxAsync(file, false));
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Log.Error("加载GPX失败", ex);
                }
            }
            GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrack.ToArray(), false));
        }

        /// <summary>
        /// 导入并加载一个轨迹
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="raiseEvent"></param>
        /// <returns></returns>
        public async Task<List<TrackInfo>> LoadGpxAsync(string filePath, bool raiseEvent)
        {
            Gpx gpx = null;
            await Task.Run(async () =>
             {
                 gpx = await Gpx.FromFileAsync(filePath);
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
                    LoadTrack(trackInfo, false, false);
                    loadedTrack.Add(trackInfo);
                }
                catch (Exception ex)
                {
                    App.Log.Error("加载gpx文件" + filePath + "的Track(" + i.ToString() + ")错误", ex);
                }
            }
            if (raiseEvent)
            {
                GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(loadedTrack.ToArray(), false));
            }

            return loadedTrack;
        }

        /// <summary>
        /// 加载一个轨迹
        /// </summary>
        /// <param name="trackInfo">轨迹信息</param>
        /// <param name="update">是否为更新，还是新建</param>
        /// <param name="raiseEvent">是否通知加载完成</param>
        /// <param name="gpxHeight">是否显示高程</param>
        public void LoadTrack(TrackInfo trackInfo, bool update = false, bool raiseEvent = false, bool? gpxHeight = null)
        {
            //如果更新的话，把原来的点和图形对应关系取消
            if (update)
            {
                foreach (var point in trackInfo.Track.Points)
                {
                    if (gpxPointAndGraphics.ContainsKey(point))
                        gpxPointAndGraphics.Remove(point);
                }
                trackInfo.Overlay.Graphics.Clear();
            }
            double minZ = 0;
            double mag = 0;
            try
            {
                //处理自动平滑
                if (Config.Instance.Gpx_AutoSmooth)
                {
                    GpxUtility.Smooth(trackInfo.Track.Points, Config.Instance.Gpx_AutoSmoothLevel, p => p.Z.Value, (p, v) => p.Z = v);
                    if (!Config.Instance.Gpx_AutoSmoothOnlyZ)
                    {
                        GpxUtility.Smooth(trackInfo.Track.Points, Config.Instance.Gpx_AutoSmoothLevel, p => p.X, (p, v) => p.X = v);
                        GpxUtility.Smooth(trackInfo.Track.Points, Config.Instance.Gpx_AutoSmoothLevel, p => p.Y, (p, v) => p.Y = v);
                    }
                    trackInfo.Smoothed = true;
                }

                //处理高程
                minZ = Config.Instance.Gpx_Height && Config.Instance.Gpx_RelativeHeight ? trackInfo.Track.Points.Min(p => p.Z.Value) : 0;
                mag = Config.Instance.Gpx_Height ? Config.Instance.Gpx_HeightExaggeratedMagnification : 1;

            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("存在没有高程信息的点", ex);
            }

            List<List<MapPoint>> mapPoints = new List<List<MapPoint>>();
            List<MapPoint> subMapPoints = new List<MapPoint>();
            mapPoints.Add(subMapPoints);
            MapPoint lastPoint = null;
            foreach (var p in trackInfo.Track.Points)
            {
                if (Config.Instance.BasemapCoordinateSystem != CoordinateSystem.WGS84)
                {
                    var newP = CoordinateTransformation.Transformate(p.ToMapPoint(), CoordinateSystem.WGS84, Config.Instance.BasemapCoordinateSystem);
                    p.X = newP.X;
                    p.Y = newP.Y;
                    p.Z = newP.Z;
                }

                MapPoint point = new MapPoint(p.X, p.Y, (p.Z.Value - minZ) * mag, SpatialReferences.Wgs84);

                //如果前后两个点离得太远了，那么就不连接
                if (lastPoint != null && GeometryUtility.GetDistance(point, lastPoint) > Config.Instance.Gpx_MaxAcceptablePointDistance)
                {
                    subMapPoints = new List<MapPoint>();
                    mapPoints.Add(subMapPoints);
                }
                subMapPoints.Add(point);
                lastPoint = point;


                Graphic graphic = new Graphic(point);
                if (!gpxPointAndGraphics.ContainsKey(p))
                {
                    gpxPointAndGraphics.Add(p, graphic);
                }

                trackInfo.Overlay.Graphics.Add(graphic);
            }

            //创建Graphic
            Graphic lineGraphic = new Graphic(new Polyline(mapPoints));
            trackInfo.Overlay.Graphics.Insert(0, lineGraphic);
            trackInfo.Overlay.SceneProperties.SurfacePlacement =
                gpxHeight == true || !gpxHeight.HasValue && Config.Instance.Gpx_Height
                ? SurfacePlacement.Absolute
                : SurfacePlacement.DrapedFlat;
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

        /// <summary>
        /// 重新加载轨迹
        /// </summary>
        /// <param name="track"></param>
        /// <param name="raiseEvent"></param>
        /// <returns></returns>
        public async Task<List<TrackInfo>> ReloadGpxAsync(TrackInfo track, bool raiseEvent)
        {
            Tracks.Remove(track);
            var ts = await LoadGpxAsync(track.FilePath, false);
            if (raiseEvent)
            {
                GpxLoaded?.Invoke(this, new GpxLoadedEventArgs(ts.ToArray(), true));
            }
            return ts;
        }

        /// <summary>
        /// 选择一个点
        /// </summary>
        /// <param name="point"></param>
        public void SelectPoint(GpxPoint point)
        {
            SelectPoint(gpxPointAndGraphics[point]);
        }

        /// <summary>
        /// 选择一些点
        /// </summary>
        /// <param name="points"></param>
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

        /// <summary>
        /// 选择某个轨迹开头到指定点之间的所有点
        /// </summary>
        /// <param name="point"></param>
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
            if (index < 0)
            {
                return;
            }

            //选中之前的所有点
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

            //不选之后的所有点
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

        /// <summary>
        /// 游览模式中的点
        /// </summary>
        /// <param name="p"></param>
        public void SetLocation(MapPoint p)
        {
            if (browseOverlay == null)
            {
                browseOverlay = new GraphicsOverlay();
                GraphicsOverlays.Add(browseOverlay);
                browseOverlay.Renderer = BrowsePointRenderer;
            }
            var point = new MapPoint(p.X, p.Y, p.Z);
            if (browseOverlay.Graphics.Count == 0)
            {
                browseOverlay.Graphics.Add(new Graphic() { Geometry = point });
            }
            else
            {
                browseOverlay.Graphics[0].Geometry = point;
            }
        }

        /// <summary>
        /// 不选择某个点
        /// </summary>
        /// <param name="g"></param>
        public void UnselectPoint(Graphic g)
        {
            g.Symbol = NotSelectedPointSymbol;
            selectedGraphics.Remove(g);
        }

        /// <summary>
        /// 鼠标右键按下
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            mouseRightDownPosition = e.GetPosition(this);
            base.OnPreviewMouseRightButtonDown(e);
        }

        /// <summary>
        /// 鼠标右键抬起
        /// </summary>
        /// <param name="e"></param>
        protected async override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);
            var distance = Math.Sqrt(Math.Pow(e.GetPosition(this).X - mouseRightDownPosition.X, 2)
                + Math.Pow(e.GetPosition(this).Y - mouseRightDownPosition.Y, 2));
            if (distance < 5)
            {
                var location = (await ScreenToLocationAsync(e.GetPosition(this))).ToWgs84();
                ContextMenu menu = new ContextMenu();

                MenuItem item = new MenuItem()
                {
                    Header = LocationMenuUtility.GetLocationMenuString(location),
                };
                item.Click += (s, e) =>
                {
                    Clipboard.SetText(LocationMenuUtility.GetLocationClipboardString(location));
                    SnakeBar.Show("已复制经纬度到剪贴板");
                };
                menu.Items.Add(item);
                menu.IsOpen = true;
            }
        }

        /// <summary>
        /// 地图加载完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            if (Tracks != null)
            {
                Tracks.CollectionChanged += TracksCollectionChanged;
            }
            await this.LoadBaseGeoViewAsync(Config.Instance.EnableBasemapCache);
        }

        /// <summary>
        /// 地图按下时，识别选择的轨迹或点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 选择点
        /// </summary>
        /// <param name="g"></param>
        private void SelectPoint(Graphic g)
        {
            g.Symbol = SelectedPointSymbol;
            selectedGraphics.Add(g);
        }

        /// <summary>
        /// 轨迹集合发生改变，同步修改GPX点和图形的对应关系，以及覆盖层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TracksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    foreach (TrackInfo item in e.OldItems)
                    {
                        foreach (var point in item.Track.Points)
                        {
                            gpxPointAndGraphics.Remove(point);
                        }
                        GraphicsOverlays.Remove(item.Overlay);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    GraphicsOverlays.Clear();
                    gpxPointAndGraphics.Clear();
                    break;
            }
        }

        /// <summary>
        /// GPX加载完成事件
        /// </summary>
        public class GpxLoadedEventArgs : EventArgs
        {
            public GpxLoadedEventArgs(TrackInfo[] track, bool update)
            {
                Track = track;
                Update = update;
            }

            public TrackInfo[] Track { get; private set; }
            public bool Update { get; private set; }
        }

        public class PointSelectedEventArgs : EventArgs
        {
            public PointSelectedEventArgs(TrackInfo trajectory, GpxPoint point)
            {
                Trajectory = trajectory;
                Point = point;
            }

            public GpxPoint Point { get; private set; }
            public TrackInfo Trajectory { get; private set; }
        }
    }
}