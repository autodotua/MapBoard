using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Control.Dialog;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MapBoard.TileDownloaderSplicer
{
    public class ArcMapView : MapView
    {
        public ArcMapView()
        {
            Loaded += ArcMapViewLoaded;
            GeoViewTapped += MapViewTapped;
            AllowDrop = true;
            IsAttributionTextVisible = false;
            GraphicsOverlays.Add(overlay);
            graphic.Geometry = point;
            graphic.IsVisible = false;
            graphic.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Color.FromArgb(0x77, 0xFF, 0x00, 0x00), new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Green, 5));
            overlay.Graphics.Add(graphic);


            Config.Instance.UrlCollection.PropertyChanged += (p1, p2) =>
              {
                  if (p2.PropertyName == nameof(Config.UrlCollection.SelectedUrl))
                  {
                      Load().Wait();
                  }
              };
        }
        private void MapViewTapped(object sender, GeoViewInputEventArgs e)
        {

        }
        public bool IsLocal { get; set; } = false;


        Layer baseLayer;
        bool loaded = false;
        private async void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                return;
            }
            if (!IsLocal)
            {
                await Load();
            }
        }

        public async Task Load()
        {

            loaded = true;
            try
            {
                //var baseLayer = new WebTiledLayer("http://online{num}.map.bdimg.com/tile/?qt=tile&x={col}&y={row}&z={level}&styles=pl&scaler=1&udt=20141103", new string[] { "1", "2", "3", "4" });
                //baseLayer = new WebTiledLayer("file:/C:/Users/autod/Desktop/瓦片下载拼接器Debug/Download/{level}/{col}-{row}.png");
                if (IsLocal)
                {
                    if (Map != null)
                    {
                        return;
                    }
                    baseLayer = new WebTiledLayer("http://127.0.0.1:8080/{col}-{row}-{level}");
                }
                else
                {
                    if (Config.Instance.UrlCollection.SelectedUrl == null || Config.Instance.UrlCollection.SelectedUrl.Url == null)
                    {
                        return;
                    }
                    //baseLayer = new WmtsLayer(new Uri("http://t0.tianditu.gov.cn/vec_c/wmts?tk=9396357d4b92e8e197eafa646c3c541d"), "vec_c");
                    //baseLayer = new WebTiledLayer();

                    baseLayer = new WebTiledLayer(Config.Instance.UrlCollection.SelectedUrl.Url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
                }
                Basemap basemap = new Basemap(baseLayer);
                //Basemap basemap = new Basemap(new WebTiledLayer("http://webrd0{subDomain}.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=7&x={col}&y={row}&z={level}", new string[] { "1", "2", "3", "4" }));
                await basemap.LoadAsync();
                if (Map == null)
                {
                    Map = new Map(basemap);

                    await Map.LoadAsync();
                    //await SetViewpointCenterAsync(new MapPoint(13532000, 3488400));
                    //await SetViewpointScaleAsync(1000000);
                }
                else
                {
                    Map.Basemap = basemap;
                }
                
            }
            catch (Exception ex)
            {
                TaskDialog.ShowException( ex, "加载地图失败");
                return;
            }
        }
        bool isSelecting = false;
        public async Task StartSelectAsync()
        {
            if (!isSelecting)
            {
                SnakeBar.Show("开始框选");
                if (SketchEditor == null)
                {
                    SketchEditor = new SketchEditor();
                }
                isSelecting = true;
                await SketchEditor.StartAsync(SketchCreationMode.Rectangle);
                isSelecting = false;
            }
            else
            {
                SketchEditor.Stop();
                SnakeBar.Show("终止框选");
            }

        }



        protected async override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (SketchEditor != null && SketchEditor.Geometry != null)
            {
                await Task.Delay(500);
                Boundary = GeometryEngine.Project(SketchEditor.Geometry, SpatialReferences.Wgs84).Extent;
                SelectBoundaryComplete?.Invoke(this, new EventArgs());
            }
        }
        public Envelope Boundary { get; private set; }

        public event EventHandler SelectBoundaryComplete;

        MapPoint point = new MapPoint(0, 0);
        GraphicsOverlay overlay = new GraphicsOverlay();
        Graphic graphic = new Graphic();
        public void ShowPosition(DownloadFileInfo file)
        {
            if (file == null)
            {
                graphic.IsVisible = false;
                return;
            }

            MapPoint wn = new MapPoint(file.WestNorth.X, file.WestNorth.Y, SpatialReferences.Wgs84);
            MapPoint en = new MapPoint(file.EastSouth.X, file.WestNorth.Y, SpatialReferences.Wgs84);
            MapPoint es = new MapPoint(file.EastSouth.X, file.EastSouth.Y, SpatialReferences.Wgs84);
            MapPoint ws = new MapPoint(file.WestNorth.X, file.EastSouth.Y, SpatialReferences.Wgs84);

            Polygon polygon = new Polygon(new MapPoint[] { wn, en, es, ws });
            //polygon = GeometryEngine.Project(polygon, SpatialReferences.WebMercator)  as Polygon;


            graphic.IsVisible = true;
            graphic.Geometry = polygon;


        }

    }

}
