using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Map;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using static FzLib.UI.Common;

namespace MapBoard.Main.Util
{
    public static class IOUtility
    {
        public async static Task ImportFeatureAsync()
        {
            LayerInfo layer = LayerCollection.Instance.Selected;
            FileFilterCollection filter = null;

            if (layer.Type != GeometryType.Polygon)
            {
                filter = new FileFilterCollection()
                    .Add("CSV表格", "csv")
                    .Add("GPS轨迹文件", "gpx")
                    .AddUnion();
            }
            else
            {
                filter = new FileFilterCollection()
                  .Add("CSV表格", "csv");
            }
            string path = FileSystemDialog.GetOpenFile(filter);
            try
            {
                switch (Path.GetExtension(path))
                {
                    case ".gpx":
                        Feature[] features = await Gpx.ImportToLayerAsync(path, layer);
                        if (features.Length > 1)
                        {
                            SnakeBar.Show("导入GPX成功");
                        }
                        else
                        {
                            SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner);
                            snake.ShowButton = true;
                            snake.ButtonContent = "查看";
                            snake.ButtonClick += async (p1, p2) => await ArcMapView.Instance.ZoomToGeometryAsync(GeometryEngine.Project(features[0].Geometry.Extent, SpatialReferences.WebMercator));

                            snake.ShowMessage("已导出到" + path);
                        }
                        break;

                    case ".csv":
                        await Csv.ImportAsync(path);
                        SnakeBar.Show("导入CSV成功");
                        break;
                }
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导入失败");
            }
        }

        public async static Task ExportLayerAsync(LayerInfo layer)
        {
            System.Diagnostics.Debug.Assert(layer != null);
            string path = FileSystemDialog.GetSaveFile(new FileFilterCollection()
                .Add("地图画板图层包", "mblpkg")
                .Add("GIS工具箱图层", "zip")
                .Add("KML打包文件", "kmz")
                , true, layer.Name);
            if (path != null)
            {
                try
                {
                    SnakeBar.Show("正在导出，请勿关闭程序");
                    await (App.Current.MainWindow as MainWindow).DoAsync(async () =>
                     {
                         switch (Path.GetExtension(path))
                         {
                             case ".mblpkg":
                                 await Package.ExportLayer2Async(path, layer);
                                 break;

                             case ".zip":
                                 await MobileGISToolBox.ExportLayerAsync(path, layer);
                                 break;

                             case ".kmz":
                                 await Kml.ExportAsync(path, layer);
                                 break;

                             default:
                                 throw new Exception("未知文件类型");
                         }
                     });
                    SnakeBar.Show(App.Current.MainWindow, "导出成功");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
                }
            }
        }

        /// <summary>
        /// 显示对话框导入
        /// </summary>
        /// <returns>返回是否需要通知刷新Style</returns>
        public async static Task ImportLayerAsync()
        {
            bool ok = true;
            string path = FileSystemDialog.GetOpenFile(new FileFilterCollection()

                    .Add("支持的格式", "mbmpkg,mblpkg,gpx,shp")
                   .Add("mbmpkg地图画板包", "mbmpkg")
                    .Add("mblpkg地图画板图层包", "mblpkg")
                   .Add("GPS轨迹文件", "gpx")
                 .Add("Shapefile文件", "shp"));

            if (path != null)
            {
                try
                {
                    switch (Path.GetExtension(path))
                    {
                        case ".mbmpkg":
                            await Package.BackupAsync();
                            Package.ImportMapAsync(path);
                            return;

                        case ".mblpkg":
                            await Package.ImportLayerAsync(path);
                            return;

                        case ".gpx":
                            await ImportGpxAsync(new[] { path });
                            break;

                        case ".shp":
                            await Shapefile.ImportAsync(path);
                            return;

                        default:
                            throw new Exception("未知文件类型");
                    }
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导入失败");
                    ok = false;
                }
                finally
                {
                    if (ok)
                    {
                        SnakeBar.Show("导入成功");
                    }
                }
            }
            return;
        }

        /// <summary>
        /// 显示对话框导出
        /// </summary>
        /// <returns></returns>
        public static async Task ExportMapAsync()
        {
            string path = FileSystemDialog.GetSaveFile(new FileFilterCollection()
                   .Add("mbmpkg地图画板包", "mbmpkg")
                .Add("GIS工具箱图层", "zip")
                 .Add("截图", "png")
                .Add("KML打包文件", "kmz"),
                 true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (path != null)
            {
                try
                {
                    SnakeBar.Show("正在导出，请勿关闭程序");
                    await (App.Current.MainWindow as MainWindow).DoAsync(async () =>
                    {
                        switch (Path.GetExtension(path))
                        {
                            case ".mbmpkg":
                                await Package.ExportMap2Async(path);
                                break;

                            case ".png":
                                await SaveImageAsync(path);
                                break;

                            case ".zip":
                                await MobileGISToolBox.ExportMapAsync(path);
                                break;

                            case ".kmz":
                                await Kml.ExportAsync(path);
                                break;

                            default:
                                throw new Exception("未知文件类型");
                        }
                    });
                    SnakeBar.Show(App.Current.MainWindow, "导出成功");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
                }
            }
        }

        /// <summary>
        /// 保存截图
        /// </summary>
        /// <param name="path">保存地址</param>
        /// <returns></returns>
        private static async Task SaveImageAsync(string path)
        {
            RuntimeImage image = await ArcMapView.Instance.ExportImageAsync();
            Bitmap bitmap = ConvertToBitmap(await image.ToImageSourceAsync() as BitmapSource);
            bitmap.Save(path);
            Bitmap ConvertToBitmap(BitmapSource bitmapSource)
            {
                var width = bitmapSource.PixelWidth;
                var height = bitmapSource.PixelHeight;
                var stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
                var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
                bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
                return new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, memoryBlockPointer);
            }
        }

        private static async Task ImportGpxAsync(string[] files)
        {
            List<DialogItem> items = new List<DialogItem>()
                {
                       new DialogItem("使用GPX工具箱打开","使用GPX工具箱打开该轨迹",()=>new GpxToolbox.UI.MainWindow(files).Show()),
                        new DialogItem("导入到新图层（线）","每一个文件将会生成一条线",async()=>await Gpx.ImportAllToNewLayerAsync(files,Gpx.GpxImportType.Line)),
                        new DialogItem("导入到新图层（点）","生成所有文件的轨迹点",async()=>await Gpx.ImportAllToNewLayerAsync(files,Gpx.GpxImportType.Point)),
                };
            if (LayerCollection.Instance.Selected != null)
            {
                var layer = LayerCollection.Instance.Selected;
                if (layer.Type == GeometryType.Point || layer.Type == GeometryType.Polyline)
                {
                    items.Add(new DialogItem("导入到当前图层", "将轨迹导入到当前图层", async () => await Gpx.ImportToLayersAsync(files, layer)));
                }

                await CommonDialog.ShowSelectItemDialogAsync("选择打开多个GPX文件的方式", items);
            }
        }

        public async static Task DropFilesAsync(string[] files)
        {
            if (files.Count(p => p.EndsWith(".gpx")) == files.Length)
            {
                await ImportGpxAsync(files);
            }
            else if (files.Count(p => p.EndsWith(".mbmpkg")) == files.Length && files.Length == 1)
            {
                if (await CommonDialog.ShowYesNoDialogAsync("是否覆盖当前所有样式？") == true)
                {
                    Package.ImportMapAsync(files[0]);
                }
            }
            else if (files.Count(p => p.EndsWith(".mblpkg")) == files.Length)
            {
                if (await CommonDialog.ShowYesNoDialogAsync("是否导入图层？") == true)
                {
                    foreach (var file in files)
                    {
                        await Package.ImportLayerAsync(file);
                        await Task.Delay(500);
                    }
                }
            }
            else if (files.Count(p => p.EndsWith(".csv")) == files.Length)
            {
                if (await CommonDialog.ShowYesNoDialogAsync("是否导入CSV文件？") == true)
                {
                    foreach (var file in files)
                    {
                        await IO.Csv.ImportAsync(file);
                    }
                }
            }
            else
            {
                SnakeBar.ShowError("不支持的文件格式，文件数量过多，或文件集合的类型不都一样");
            }
        }
    }
}