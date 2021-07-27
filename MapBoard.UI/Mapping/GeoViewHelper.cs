using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib;
using MapBoard.Model;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace MapBoard.Mapping
{
    public static class GeoViewHelper
    {
        /// <summary>
        /// 为ArcSceneView或ArcMapView加载底图
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public async static Task<ItemsOperationErrorCollection> LoadBaseGeoViewAsync(this GeoView map)
        {
            ItemsOperationErrorCollection errors = new ItemsOperationErrorCollection();
            try
            {
                Basemap basemap = new Basemap();

                foreach (var item in Config.Instance.BaseLayers.Reverse<BaseLayerInfo>())
                {
                    var layer = GetLayer(item);
                    try
                    {
                        await layer.LoadAsync().TimeoutAfter(Parameters.LoadTimeout);
                        basemap.BaseLayers.Add(layer);

                        layer.Opacity = item.Opacity;
                        layer.Id = item.TempID.ToString();
                        layer.IsVisible = item.Enable;
                    }
                    catch (TimeoutException ex)
                    {
                        layer.CancelLoad();
                        errors.Add($"加载底图{item.Name}({item.Path})超时", ex);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"加载底图{item.Name}({item.Path})失败", ex);
                    }
                }

                if (basemap.BaseLayers.Count == 0)
                {
                    basemap = new Basemap(new RasterLayer(Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "res", "DefaultBaseMap.jpg")));
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
                throw new Exception("加载底图失败", ex);
            }
        }

        public static void SetHideWatermark(this GeoView map)
        {
            map.Margin = GetMinusWatermarkThickness();
        }

        public static Thickness GetWatermarkThickness()
        {
            if (!Config.Instance.HideWatermark)
            {
                return new Thickness();
            }
            return new Thickness(0, Config.WatermarkHeight, 0, Config.WatermarkHeight); ;
        }

        public static Thickness GetMinusWatermarkThickness()
        {
            if (!Config.Instance.HideWatermark)
            {
                return new Thickness();
            }
            return new Thickness(0, -Config.WatermarkHeight, 0, -Config.WatermarkHeight); ;
        }

        public const string WebTiledLayer = nameof(WebTiledLayer);
        public const string RasterLayer = nameof(RasterLayer);
        public const string ShapefileLayer = nameof(ShapefileLayer);
        public const string TpkLayer = nameof(TpkLayer);

        public static Layer GetLayer(BaseLayerInfo baseLayer)
        {
            var type = baseLayer.Type;
            var arg = baseLayer.Path;
            return type switch
            {
                BaseLayerType.WebTiledLayer => AddTiledLayer(arg),
                BaseLayerType.RasterLayer => AddRasterLayer(arg),
                BaseLayerType.ShapefileLayer => AddShapefileLayer(arg),
                BaseLayerType.TpkLayer => AddTpkLayer(arg),
                BaseLayerType.WmsLayer => AddWmsLayer(arg),
                _ => throw new Exception("未知类型"),
            };
        }

        private static WebTiledLayer AddTiledLayer(string url)
        {
            WebTiledLayer layer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
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

        private static RasterLayer AddRasterLayer(string path)
        {
            RasterLayer layer = new RasterLayer(path);
            return layer;
        }

        private static ArcGISTiledLayer AddTpkLayer(string path)
        {
            TileCache cache = new TileCache(path);
            ArcGISTiledLayer layer = new ArcGISTiledLayer(cache);
            return layer;
        }

        private static FeatureLayer AddShapefileLayer(string path)
        {
            ShapefileFeatureTable table = new ShapefileFeatureTable(path);
            FeatureLayer layer = new FeatureLayer(table);
            return layer;
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

        public async static Task ExportImageAsync(this GeoView view, string path, ImageFormat format, Thickness cut)
        {
            var result = await view.GetImageAsync(cut);
            result.Save(path, format);
        }

        public async static Task<Bitmap> GetImageAsync(this GeoView view, Thickness cut)
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
    }
}