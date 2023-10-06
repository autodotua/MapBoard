using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Maui;
using MapBoard.Model;
using System.ComponentModel;
using FzLib;
using System.Text.RegularExpressions;
using Map = Esri.ArcGISRuntime.Mapping.Map;
using Esri.ArcGISRuntime.UI;
using static MapBoard.Util.GeometryUtility;

namespace MapBoard.Mapping
{
    public static class MapViewHelper
    {
        public static List<BaseLayerInfo> BaseLayers { get; set; } = new List<BaseLayerInfo>()
        {
            new BaseLayerInfo()
            {
                Name="xyz",
                Type=BaseLayerType.WebTiledLayer,
                Path="https://autodotua.top/map/google/{z}/{x}-{y}.jpg"
            }
        };
        private static Regex rColorRamp = new Regex(@$"(?<type>{nameof(PresetColorRampType.Elevation)}|{nameof(PresetColorRampType.DemScreen)}|{nameof(PresetColorRampType.DemLight)})(,(?<size>[0-9]+))?");

        private static Regex rMinMaxStretch = new Regex(@"m(inmax)?\((?<min>[0-9\.:]+),(?<max>[0-9\.:]+)\)");

        private static Regex rPercentStretch = new Regex(@"p(ercent)?\((?<min>[0-9\.]+),(?<max>[0-9\.]+)\)");

        private static Regex rRgbRenderer = new Regex(@"r(gb)?\((?<r>[0-9\.]+),(?<g>[0-9\.]+),(?<b>[0-9\.]+)\)");

        private static Regex rStdStretch = new Regex(@"s(td)?\((?<factor>[0-9\.]+)\)");

        private static Regex rStretchRenderer = new Regex(@"s(tretch)?");


        /// <summary>
        /// 从文件读取预设的底图
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static IEnumerable<BaseLayerInfo> GetDefaultBaseLayers()
        {
            string defaultBasemapLayersPath = Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "res", "DefaultBasemapLayers.txt");

            if (File.Exists(defaultBasemapLayersPath))
            {
                return File.ReadAllLines(defaultBasemapLayersPath)
                    .Select(p => p.Trim().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(p => p.Length == 2 ?
                    new BaseLayerInfo(BaseLayerType.WebTiledLayer, p[1]) { Name = p[0] }
                    : throw new FormatException("默认底图文件格式错误"));
            }
            return Enumerable.Empty<BaseLayerInfo>();
        }


        /// <summary>
        /// 根据应用的<see cref="BaseLayerInfo"/>对象，获取ArcGIS的对应图层
        /// </summary>
        /// <param name="baseLayer"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        public static Layer GetLayer(BaseLayerInfo baseLayer, bool cache)
        {
            var type = baseLayer.Type;
            var arg = baseLayer.Path;
            return type switch
            {
                BaseLayerType.WebTiledLayer => AddTiledLayer(arg, cache),
                BaseLayerType.RasterLayer => AddRasterLayer(arg),
                BaseLayerType.ShapefileLayer => AddShapefileLayer(arg),
                BaseLayerType.TpkLayer => AddTpkLayer(arg),
                BaseLayerType.WmsLayer => AddWmsLayer(arg),
                BaseLayerType.WmtsLayer => AddWmtsLayer(arg),
                _ => throw new InvalidEnumArgumentException("未知类型"),
            };
        }


        /// <summary>
        /// 为<see cref="SceneView"/>或<see cref="MapView"/>加载底图
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public static async Task<ItemsOperationErrorCollection> LoadBaseGeoViewAsync(this GeoView map, bool cache)
        {
            ItemsOperationErrorCollection errors = new ItemsOperationErrorCollection();
            try
            {
                Basemap basemap = new Basemap();
                await basemap.LoadAsync();
                if (BaseLayers.Count == 0)
                {
                    //如果没有底图，一般是首次打开软件的时候，那么从默认瓦片底图的文件中读取第一条加载
                    try
                    {
                        var defaultBaseLayers = GetDefaultBaseLayers();
                        if (defaultBaseLayers.Any())
                        {
                            BaseLayers.Add(defaultBaseLayers.First());
                        }
                    }
                    catch
                    {
                    }
                }
                //加载底图
                var baseLayers = BaseLayers.Where(p => p.Enable).Reverse();
                foreach (var item in baseLayers)
                {
                    Layer layer = null;
                    try
                    {
                        if (item.Type is BaseLayerType.RasterLayer or BaseLayerType.ShapefileLayer or BaseLayerType.TpkLayer && !File.Exists(item.Path))
                        {
                            throw new IOException("找不到文件：" + item.Path);
                        }
                        layer = GetLayer(item, cache);
                        await layer.LoadAsync().TimeoutAfter(Parameters.LoadTimeout);
                        basemap.BaseLayers.Add(layer);

                        item.ApplyBaseLayerStyles(layer);
                    }
                    catch (TimeoutException ex)
                    {
                        layer?.CancelLoad();
                        //App.Log.Error($"加载底图{item.Name}({item.Path})超时", ex);
                        errors.Add($"加载底图{item.Name}({item.Path})超时", ex);
                    }
                    catch (Exception ex)
                    {
                        //App.Log.Error($"加载底图{item.Name}({item.Path})失败", ex);
                        errors.Add($"加载底图{item.Name}({item.Path})失败", ex);
                    }
                }
                //如果还是没有底图，那么加载单张世界地图
                if (basemap.BaseLayers.Count == 0)
                {
                    string defaultBaseMapPath = Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "res", "DefaultBaseMap.jpg");
                    basemap = new Basemap(new RasterLayer(defaultBaseMapPath));
                }
                await basemap.LoadAsync();
                if (map is SceneView s)
                {
                    if (s.Scene == null)
                    {
                        s.Scene = basemap.BaseLayers.Count == 0 ? new Scene() : new Scene(basemap);
                    }
                    else
                    {
                        s.Scene.Basemap = basemap;
                    }
                    await s.Scene.LoadAsync();
                }
                else if (map is MapView m)
                {
                    if (m.Map == null)
                    {
                        m.Map = basemap.BaseLayers.Count == 0 ? new Map(SpatialReferences.Wgs84) : new Map(basemap);
                    }
                    else
                    {
                        m.Map.Basemap = basemap;
                    }
                    await m.Map.LoadAsync();
                }
                return errors.Count == 0 ? null : errors;
            }
            catch (Exception ex)
            {
                //App.Log.Error("加载底图失败", ex);
                throw new Exception("加载底图失败", ex);
            }
        }


        /// <summary>
        /// 等待渲染结束
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public static Task WaitForRenderCompletedAsync(this GeoView view)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();

            if (view.DrawStatus == DrawStatus.Completed)
            {
                tcs.SetResult();
                return tcs.Task;
            }
            view.DrawStatusChanged += View_DrawStatusChanged;
            return tcs.Task;

            void View_DrawStatusChanged(object sender, DrawStatusChangedEventArgs e)
            {
                if (e.Status == DrawStatus.Completed)
                {
                    view.DrawStatusChanged -= View_DrawStatusChanged;
                    tcs.SetResult();
                }
            }
        }

        private static RasterLayer AddRasterLayer(string path)
        {
            RasterLayer layer = new RasterLayer(path);
            return layer;
        }

        private static FeatureLayer AddShapefileLayer(string path)
        {
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            FeatureLayer layer = new FeatureLayer(table);
            return layer;
        }
        private static string HttpUserAgent = "";
        private static ImageTiledLayer AddTiledLayer(string url, bool cache)
        {
            XYZTiledLayer layer = XYZTiledLayer.Create(url, HttpUserAgent, cache);
            //WebTiledLayer layer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
            return layer;
        }

        private static ArcGISTiledLayer AddTpkLayer(string path)
        {
            TileCache cache = new TileCache(path);
            ArcGISTiledLayer layer = new ArcGISTiledLayer(cache);
            return layer;
        }

        private static WmsLayer AddWmsLayer(string url)
        {
            string[] items = url.Split('|');
            if (items.Length < 2)
            {
                throw new ArgumentException("WMS的参数过少");
            }
            WmsLayer layer = new WmsLayer(new Uri(items[0]), items.Skip(1));
            return layer;
        }

        private static WmtsLayer AddWmtsLayer(string url)
        {
            string[] items = url.Split('|');
            if (items.Length < 2)
            {
                throw new ArgumentException("WMS的参数过少");
            }
            WmtsLayer layer = new WmtsLayer(new Uri(items[0]), items[1]);
            return layer;
        }

        /// <summary>
        /// 应用底图样式，包括透明度等、亮度等、拉伸、渲染器、缩放等级。
        /// </summary>
        /// <param name="baseLayer"></param>
        /// <param name="arcLayer"></param>
        public static void ApplyBaseLayerStyles(this BaseLayerInfo baseLayer, Layer arcLayer)
        {
            arcLayer.Opacity = baseLayer.Opacity;
            arcLayer.Id = baseLayer.TempID.ToString();
            arcLayer.IsVisible = baseLayer.Visible;

            if (arcLayer is ImageAdjustmentLayer i)
            {
                i.Gamma = baseLayer.Gamma;
                i.Brightness = baseLayer.Brightness;
                i.Contrast = baseLayer.Contrast;
            }

            if (arcLayer is RasterLayer raster)
            {
                if (string.IsNullOrEmpty(baseLayer.Renderer))
                {
                    return;
                }
                if (string.IsNullOrEmpty(baseLayer.StretchParameters))
                {
                    return;
                }
                RasterRenderer renderer = null;
                StretchParameters stretchParameters = null;
                ColorRamp colorRamp = null;
                Match match = null;

                if (baseLayer.ColorRampParameters != null)
                {
                    try
                    {
                        if (rColorRamp.IsMatch(baseLayer.ColorRampParameters))
                        {
                            match = rColorRamp.Match(baseLayer.ColorRampParameters);
                            var type = Enum.Parse<PresetColorRampType>(match.Groups["type"].Value);
                            uint size = 256;
                            if (match.Groups.ContainsKey("size") && match.Groups["size"].Value != "")
                            {
                                size = uint.Parse(match.Groups["size"].Value);
                            }
                            colorRamp = ColorRamp.Create(type, size);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("颜色渐变参数错误");
                    }
                }
                try
                {
                    if (rMinMaxStretch.IsMatch(baseLayer.StretchParameters))
                    {
                        match = rMinMaxStretch.Match(baseLayer.StretchParameters);
                        var mins = match.Groups["min"].Value.Split(':').Select(double.Parse);
                        var maxs = match.Groups["max"].Value.Split(':').Select(double.Parse);
                        stretchParameters = new MinMaxStretchParameters(mins, maxs);
                    }
                    else if (rStdStretch.IsMatch(baseLayer.StretchParameters))
                    {
                        match = rStdStretch.Match(baseLayer.StretchParameters);
                        var factor = double.Parse(match.Groups["factor"].Value);
                        stretchParameters = new StandardDeviationStretchParameters(factor);
                    }
                    else if (rPercentStretch.IsMatch(baseLayer.StretchParameters))
                    {
                        match = rPercentStretch.Match(baseLayer.StretchParameters);
                        var min = double.Parse(match.Groups["min"].Value);
                        var max = double.Parse(match.Groups["max"].Value);
                        stretchParameters = new PercentClipStretchParameters(min, max);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("拉伸参数错误");
                }


                try
                {
                    if (rRgbRenderer.IsMatch(baseLayer.Renderer))
                    {
                        match = rRgbRenderer.Match(baseLayer.Renderer);
                        int r = int.Parse(match.Groups["r"].Value);
                        int g = int.Parse(match.Groups["g"].Value);
                        int b = int.Parse(match.Groups["b"].Value);
                        (r, g, b) = (r - 1, g - 1, b - 1);
                        renderer = new RgbRenderer(stretchParameters, new[] { r, g, b }, null, true);
                    }
                    else if (rStretchRenderer.IsMatch(baseLayer.Renderer))
                    {
                        match = rStretchRenderer.Match(baseLayer.Renderer);
                        renderer = new StretchRenderer(stretchParameters, null, true, colorRamp);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("渲染器参数错误");
                }

                raster.Renderer = renderer;


            }

            if (arcLayer is XYZTiledLayer xyz)
            {
                xyz.MinLevel = baseLayer.MinLevel;
                xyz.MaxLevel = baseLayer.MaxLevel;
            }

        }
        /// <summary>
        /// 缩放到指定图形
        /// </summary>
        /// <param name="mapView"></param>
        /// <param name="geometry"></param>
        /// <param name="autoExtent"></param>
        /// <returns></returns>
        public static async Task ZoomToGeometryAsync(this GeoView mapView, Geometry geometry, bool autoExtent = true)
        {
            if (geometry is MapPoint || geometry is Multipoint m && m.Points.Count == 1 || geometry.Extent.Width == 0 || geometry.Extent.Height == 0)
            {
                if (geometry.SpatialReference.Wkid != SpatialReferences.WebMercator.Wkid)
                {
                    geometry = geometry.Project(SpatialReferences.WebMercator);
                }
                geometry = geometry.Buffer(500);
            }
            var extent = geometry.Extent;
            if (double.IsNaN(extent.Width) || double.IsNaN(extent.Height) || extent.Width == 0 || extent.Height == 0)
            {
                return;
            }
            if (mapView is MapView mv)
            {
                await mv.SetViewpointGeometryAsync(geometry, Config.Instance.HideWatermark && autoExtent ? Config.WatermarkHeight : 0);
            }
            else if (mapView is SceneView sv)
            {
                await sv.SetViewpointAsync(new Viewpoint(geometry));
            }
            else
            {
                throw new ArgumentException("必须为MapView类或SceneView类", nameof(mapView));
            }
        }

        /// <summary>
        /// 缩放到记忆的位置
        /// </summary>
        /// <returns></returns>
        public static async Task TryZoomToLastExtent<T>(this T mapView) where T : MainMapView
        {
            if (mapView.Layers.MapViewExtentJson != null)
            {
                try
                {
                    Envelope envelope = Geometry.FromJson(mapView.Layers.MapViewExtentJson) as Envelope;
                    var point1 = new MapPoint(envelope.XMin, envelope.YMin);
                    var point2 = new MapPoint(envelope.XMax, envelope.YMax);
                    envelope = new Envelope(point1.RegularizeWebMercatorPoint(), point2.RegularizeWebMercatorPoint());
                    await mapView.ZoomToGeometryAsync(envelope, false);
                }
                catch
                {
                }
            }
        }
    }

}
