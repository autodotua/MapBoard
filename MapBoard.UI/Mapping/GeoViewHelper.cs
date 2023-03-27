using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace MapBoard.Mapping
{
    public static class GeoViewHelper
    {
        public static readonly string[] EsriBasemaps = typeof(Basemap)
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(p => p.Name.StartsWith("Create"))
                    .Select(p => p.Name[6..])
                    .ToArray();

        public static async Task ExportImageAsync(this GeoView view, string path, ImageFormat format, Thickness cut)
        {
            var result = await view.GetImageAsync(cut);
            result.Save(path, format);
        }

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

        public static Thickness GetMinusWatermarkThickness()
        {
            if (!Config.Instance.HideWatermark)
            {
                return new Thickness();
            }
            return new Thickness(0, -Config.WatermarkHeight, 0, -Config.WatermarkHeight); ;
        }

        public static Thickness GetWatermarkThickness()
        {
            if (!Config.Instance.HideWatermark)
            {
                return new Thickness();
            }
            return new Thickness(0, Config.WatermarkHeight, 0, Config.WatermarkHeight); ;
        }

        /// <summary>
        /// 为ArcSceneView或ArcMapView加载底图
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public static async Task<ItemsOperationErrorCollection> LoadBaseGeoViewAsync(this GeoView map, bool cache)
        {
            ItemsOperationErrorCollection errors = new ItemsOperationErrorCollection();
            try
            {
                Basemap basemap = GetBasemap(errors);
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
                var baseLayers = Config.Instance.BaseLayers.Where(p => p.Enable && p.Type != BaseLayerType.Esri).Reverse();
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

                        layer.Opacity = item.Opacity;
                        layer.Id = item.TempID.ToString();
                        layer.IsVisible = item.Visible;
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
        public static void SetHideWatermark(this GeoView map)
        {
            map.Margin = GetMinusWatermarkThickness();
        }
        public static Task WaitForRenderCompletedAsync(this GeoView view)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();

            if (view.DrawStatus == Esri.ArcGISRuntime.UI.DrawStatus.Completed)
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

        private static Basemap GetBasemap(ItemsOperationErrorCollection errors)

        {
            var info = Config.Instance.BaseLayers.FirstOrDefault(p => p.Type == BaseLayerType.Esri);
            if (info == null)
            {
                return new Basemap();
            }
            string type = info.Path;
            Debug.Assert(type != null);
            var methods = typeof(Basemap).GetMethods(BindingFlags.Static | BindingFlags.Public);
            var method = methods.FirstOrDefault(p => p.Name == $"Create{type}");
            if (method == null)
            {
                errors.Add(new ItemsOperationError("Esri底图", $"找不到类型{type}"));
                return new Basemap();
            }

            return method.Invoke(null, null) as Basemap;
        }
    }
}