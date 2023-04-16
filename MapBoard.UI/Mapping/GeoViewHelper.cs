using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib;
using MapBoard.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 二维或三维地图工具
    /// </summary>
    public static class GeoViewHelper
    {
        private static Regex rColorRamp = new Regex(@$"(?<type>{nameof(PresetColorRampType.Elevation)}|{nameof(PresetColorRampType.DemScreen)}|{nameof(PresetColorRampType.DemLight)})(,(?<size>[0-9]+))?");

        private static Regex rMinMaxStretch = new Regex(@"m(inmax)?\((?<min>[0-9\.:]+),(?<max>[0-9\.:]+)\)");

        private static Regex rPercentStretch = new Regex(@"p(ercent)?\((?<min>[0-9\.]+),(?<max>[0-9\.]+)\)");

        private static Regex rRgbRenderer = new Regex(@"r(gb)?\((?<r>[0-9\.]+),(?<g>[0-9\.]+),(?<b>[0-9\.]+)\)");

        private static Regex rStdStretch = new Regex(@"s(td)?\((?<factor>[0-9\.]+)\)");

        private static Regex rStretchRenderer = new Regex(@"s(tretch)?");

        /// <summary>
        /// 导出截图
        /// </summary>
        /// <param name="view"></param>
        /// <param name="path"></param>
        /// <param name="format"></param>
        /// <param name="cut"></param>
        /// <returns></returns>
        public static async Task ExportImageAsync(this GeoView view, string path, ImageFormat format, Thickness cut)
        {
            var result = await view.GetImageAsync(cut);
            result.Save(path, format);
        }

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
        /// 获取截图
        /// </summary>
        /// <param name="view"></param>
        /// <param name="cut"></param>
        /// <returns></returns>
        public static async Task<Bitmap> GetImageAsync(this GeoView view, Thickness cut)
        {
            RuntimeImage image = await view.ExportImageAsync();
            var bitmapSource = await image.ToImageSourceAsync() as BitmapSource;
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
            bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
            Bitmap result = null;
            //考虑DPI
            PresentationSource source = PresentationSource.FromVisual(view);
            int cutLeft = (int)(source.CompositionTarget.TransformToDevice.M11 * cut.Left);
            int cutRight = (int)(source.CompositionTarget.TransformToDevice.M11 * cut.Right);
            int cutTop = (int)(source.CompositionTarget.TransformToDevice.M22 * cut.Top);
            int cutBottom = (int)(source.CompositionTarget.TransformToDevice.M22 * cut.Bottom);
            await Task.Run(() =>
            {
                Bitmap bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppPArgb, memoryBlockPointer);
                if (cut.Equals(new Thickness(0)))
                {
                    result = bitmap;
                }
                Bitmap newBitmap = new Bitmap(width - cutLeft - cutRight, height - cutTop - cutBottom, PixelFormat.Format32bppPArgb);
                Graphics graphics = Graphics.FromImage(newBitmap);
                var rect = new Rectangle(0, 0, newBitmap.Width, newBitmap.Height);
                graphics.DrawImage(bitmap, rect, cutLeft, cutTop, newBitmap.Width, newBitmap.Height, GraphicsUnit.Pixel);
                graphics.Save();
                result = newBitmap;
            });
            return result;
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
        /// 获取负值的水印边框
        /// </summary>
        /// <returns></returns>
        public static Thickness GetMinusWatermarkThickness()
        {
            if (!Config.Instance.HideWatermark)
            {
                return new Thickness();
            }
            return new Thickness(0, -Config.WatermarkHeight, 0, -Config.WatermarkHeight); ;
        }

        /// <summary>
        /// 获取正值的水印边框
        /// </summary>
        /// <returns></returns>
        public static Thickness GetWatermarkThickness()
        {
            if (!Config.Instance.HideWatermark)
            {
                return new Thickness();
            }
            return new Thickness(0, Config.WatermarkHeight, 0, Config.WatermarkHeight); ;
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
                if (Config.Instance.BaseLayers.Count == 0)
                {
                    //如果没有底图，一般是首次打开软件的时候，那么从默认瓦片底图的文件中读取第一条加载
                    try
                    {
                        var defaultBaseLayers = GetDefaultBaseLayers();
                        if (defaultBaseLayers.Any())
                        {
                            Config.Instance.BaseLayers.Add(defaultBaseLayers.First());
                        }
                    }
                    catch
                    {
                    }
                }
                //加载底图
                var baseLayers = Config.Instance.BaseLayers.Where(p => p.Enable).Reverse();
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
                        App.Log.Error($"加载底图{item.Name}({item.Path})超时", ex);
                        errors.Add($"加载底图{item.Name}({item.Path})超时", ex);
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error($"加载底图{item.Name}({item.Path})失败", ex);
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
                App.Log.Error("加载底图失败", ex);
                throw new Exception("加载底图失败", ex);
            }
        }

        /// <summary>
        /// 隐藏水印
        /// </summary>
        /// <param name="map"></param>
        public static void SetHideWatermark(this GeoView map)
        {
            map.Margin = GetMinusWatermarkThickness();
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

        private static ImageTiledLayer AddTiledLayer(string url, bool cache)
        {
            XYZTiledLayer layer = XYZTiledLayer.Create(url, Config.Instance.HttpUserAgent, cache);
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
    }
}