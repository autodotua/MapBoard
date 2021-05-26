using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.WPF.Dialog;
using MapBoard.Common.BaseLayer;
using MapBoard.TileDownloaderSplicer.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MapBoard.TileDownloaderSplicer.UI
{
    public class ArcMapView : MapView
    {
        public ArcMapView()
        {
            instances.Add(this);
            Loaded += ArcMapViewLoaded;
            GeoViewTapped += MapViewTapped;
            AllowDrop = true;
            IsAttributionTextVisible = false;
            this.SetHideWatermark();

            GraphicsOverlays.Add(overlay);
            graphic.Geometry = point;
            graphic.IsVisible = false;
            graphic.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Color.FromArgb(0x77, 0xFF, 0x00, 0x00), new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Green, 12));
            overlay.Graphics.Add(graphic);
            Map = new Map(SpatialReferences.WebMercator);

            Map.LoadAsync().Wait();

            Config.Instance.UrlCollection.PropertyChanged += UrlCollectionPropertyChanged;
        }

        private static List<ArcMapView> instances = new List<ArcMapView>();
        public static IReadOnlyList<ArcMapView> Instances => instances.AsReadOnly();

        private async void UrlCollectionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.UrlCollection.SelectedUrl))
            {
                await LoadAsync();
            }
        }

        private void MapViewTapped(object sender, GeoViewInputEventArgs e)
        {
        }

        public bool IsLocal { get; set; } = false;

        private Layer baseLayer;
        private bool loaded = false;

        private async void ArcMapViewLoaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                return;
            }
            if (!IsLocal)
            {
                await LoadAsync();
            }
        }

        public async Task LoadAsync()
        {
            loaded = true;
            try
            {
                if (IsLocal)
                {
                    baseLayer = new WebTiledLayer($"http://127.0.0.1:" + Config.Instance.ServerPort + "/{col}-{row}-{level}");
                }
                else
                {
                    if (Config.Instance.UrlCollection.SelectedUrl == null || Config.Instance.UrlCollection.SelectedUrl.Url == null)
                    {
                        return;
                    }

                    baseLayer = new WebTiledLayer(Config.Instance.UrlCollection.SelectedUrl.Url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
                }
                Basemap basemap = new Basemap(baseLayer);

                await basemap.LoadAsync();

                Map.Basemap = basemap;
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "加载地图失败");
                return;
            }
        }

        private bool isSelecting = false;

        public async Task SelectAsync()
        {
            if (!isSelecting)
            {
                SnakeBar.Show("开始框选");

                isSelecting = true;
                await SketchEditor.StartAsync(SketchCreationMode.Rectangle, GetSketchEditConfiguration());
            }
            else
            {
                SketchEditor.Stop();
                SnakeBar.Show("终止框选");
                isSelecting = false;
            }
        }

        public void SetBoundary(GeoRange<double> range)
        {
            Polygon polygon = RangeToPolygon(range);
            Geometry geometry = GeometryEngine.Project(polygon, SpatialReference);
            SketchEditor.StartAsync(geometry, SketchCreationMode.Rectangle, GetSketchEditConfiguration());
            isSelecting = true;
        }

        private SketchEditConfiguration GetSketchEditConfiguration()
        {
            return new SketchEditConfiguration()
            {
                AllowRotate = false,
                ResizeMode = SketchResizeMode.Stretch,
                AllowVertexEditing = false
            };
        }

        private Polygon RangeToPolygon(GeoRange<double> range)
        {
            MapPoint wn = new MapPoint(range.XMin_Left, range.YMax_Top, SpatialReferences.Wgs84);
            MapPoint en = new MapPoint(range.XMax_Right, range.YMax_Top, SpatialReferences.Wgs84);
            MapPoint es = new MapPoint(range.XMax_Right, range.YMin_Bottom, SpatialReferences.Wgs84);
            MapPoint ws = new MapPoint(range.XMin_Left, range.YMin_Bottom, SpatialReferences.Wgs84);

            Polygon polygon = new Polygon(new MapPoint[] { wn, en, es, ws }, SpatialReferences.Wgs84);
            return polygon;
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

        private MapPoint point = new MapPoint(0, 0);
        private GraphicsOverlay overlay = new GraphicsOverlay();
        private Graphic graphic = new Graphic();

        public void ShowPosition(Window win, TileInfo tile)
        {
            if (tile == null)
            {
                graphic.IsVisible = false;
                return;
            }
            bool ignore = false;
            win.Dispatcher.Invoke(() =>
            {
                if (win.WindowState == WindowState.Minimized)
                {
                    ignore = true; ;
                }
            });
            if (ignore)
            {
                return;
            }
            var range = tile.Extent;

            graphic.IsVisible = true;
            graphic.Geometry = RangeToPolygon(range);
        }
    }
}