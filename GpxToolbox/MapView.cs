//using Esri.ArcGISRuntime.Geometry;
//using FzLib.Control.Dialog;
//using FzLib.Geography.Format;
//using GMap.NET;
//using GMap.NET.MapProviders;
//using GMap.NET.Projections;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.IO;
//using System.Windows;
//using System.Windows.Media;
//using System.Windows.Shapes;
//using ArcMapPoint = Esri.ArcGISRuntime.Geometry;

//namespace GeographicTrajectoryToolbox
//{
//    public class AMapProvider : AMapProviderBase
//    {
//        private readonly string name = "AMap";
//        private readonly string language = "zh_cn";
//        private readonly Guid id = Guid.NewGuid();// new Guid("F81F5FB4-0902-4686-BF5B-B2B1E4D47922");
//        public static readonly AMapProvider Instance;
//        private Random ran = new Random();
//        private static string UrlFormat = "http://webrd0{0}.is.autonavi.com/appmaptile?lang=zh_cn&size=1&style=7&x={1}&y={2}&z={3}&scale=1&ltype=3";


//        public string Caption
//        {
//            get
//            {
//                return "高德地图";
//            }
//        }
//        public override Guid Id
//        {
//            get { return this.id; }
//        }

//        public override string Name
//        {
//            get { return this.name; }
//        }

//        static AMapProvider()
//        {
//            Instance = new AMapProvider();
//        }
//        private AMapProvider()
//        {

//        }

//        public override PureImage GetTileImage(GPoint pos, int zoom)
//        {
//            string url = MakeTileImageUrl(pos, zoom, language);
//            return GetTileImageUsingHttp(url);
//        }
//        //http://wprd0{0}.is.autonavi.com/appmaptile?lang=zh_cn&size=1&style=7&x={1}&y={2}&z={3}&scl=2&ltype=3
//        private string MakeTileImageUrl(GPoint pos, int zoom, string language)
//        {
//            int serverID = ran.Next(1, 5);//1-4 
//            return string.Format(UrlFormat, 4, pos.X, pos.Y, zoom);
//        }
//    }


//    public abstract class AMapProviderBase : GMapProvider
//    {
//        protected GMapProvider[] overlays;
//        public AMapProviderBase()
//        {
//            RefererUrl = "http://www.amap.com/";
//            Copyright = string.Format("©{0} 高德地图 GPRS(@{0})", DateTime.Today.Year);
//            MinZoom = 1;
//            MaxZoom = 20;
//        }

//        public override GMapProvider[] Overlays
//        {
//            get
//            {
//                if (overlays == null)
//                {
//                    overlays = new GMapProvider[] { this };
//                }
//                return overlays;
//            }
//        }

//        public override PureProjection Projection
//        {
//            get
//            {
//                return MercatorProjection.Instance;
//            }
//        }
//    }
//    public class MapView : GMap.NET.WindowsPresentation.GMapControl, INotifyPropertyChanged
//    {
//        public MapView()
//        {
//            MinZoom = 1;
//            MaxZoom = 17;
//            Zoom = 10;
//            MapProvider = AMapProvider.Instance;
//            ShowCenter = false;
//            DragButton = System.Windows.Input.MouseButton.Left;
//            MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
//            Position = new PointMapPoint(30, 120);
//            AllowDrop = true;
//            GpxFiles.Add(@"E:\旧事重提\多媒体\家庭\大学\20180811灌顶水库附近意外爬山\返回.gpx");
//            LoadGpxs();
//            if (Markers.Count > 0)
//            {
//                CenterPosition = Markers[0].Position;
//                Zoom = 15;
//            }

//        }
//        protected override void OnDragEnter(DragEventArgs e)
//        {
//            base.OnDragEnter(e);
//            if (e.Data.GetDataPresent(DataFormats.FileDrop))
//            {
//                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
//                if (files.Length == 1 && files[0].EndsWith(".gpx"))
//                {
//                    e.Effects = DragDropEffects.Link;
//                }
//            }

//        }

//        protected override void OnDrop(DragEventArgs e)
//        {
//            base.OnDrop(e);
//            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
//            if (files.Length == 1 && files[0].EndsWith(".gpx"))
//            {
//                if (new FileInfo(files[0]).Length > 10 * 1024 * 1024)
//                {
//                    DialogBox.ShowError("文件大于10MB");
//                    return;
//                }
//                GpxFiles.Add(files[0]);
//            }
//            LoadGpxs();

//            if (Markers.Count > 0)
//            {
//                CenterPosition = Markers[0].Position;
//                Zoom = 15;
//            }
//        }

//        public void LoadGpxs()
//        {
//            Markers.Clear();
//            foreach (var file in GpxFiles)
//            {
//                LoadGpx(File.ReadAllText(file));
//            }
//        }
//        private void LoadGpx(string gpxContent)
//        {
//            GpxInfo info = GpxInfo.FromString(gpxContent);
//            List<MapPoint> points = new List<MapPoint>(); ;
//            foreach (var point in info.Tracks[0].Points)
//            {

//                var newP = FzLib.Geography.Coordinate.Convert.WGS84ToGCJ02(new FzLib.Geography.Coordinate.MapPoint(point.Latitude, point.Longitude));
//                points.Add(new MapPoint() { Longitude = newP.Longitude, Latitude = newP.Latitude });
//            }



//            // List<MapPoint> points = info.Tracks[0].GetOffsetPoints(OffsetNorth, OffsetEast);
//            foreach (var item in points)
//            {
//                Markers.Add(new GMap.NET.WindowsPresentation.GMapMarker(new PointMapPoint(item.Latitude, item.Longitude))
//                {
//                    Shape = new Ellipse
//                    {
//                        Width = 10,
//                        Height = 10,
//                        Fill = Brushes.Red,
//                    }
//                });
//            }




//        }
//        private double offsetNorth = 0;
//        private double offsetEast = 0;
//        public double OffsetNorth
//        {
//            get => offsetNorth;
//            set
//            {
//                offsetNorth = value;
//                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OffsetNorth)));
//            }
//        }
//        public double OffsetEast
//        {
//            get => offsetEast;
//            set
//            {
//                offsetEast = value;
//                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OffsetEast)));
//            }
//        }
//        public event PropertyChangedEventHandler PropertyChanged;

//        public List<string> GpxFiles { get; } = new List<string>();
//    }
//}
