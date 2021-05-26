using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.WPF.Dialog;
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

namespace MapBoard.Main.Util
{
    public static class IOUtility
    {
        public async static Task ImportFeatureAsync(LayerInfo layer, ArcMapView mapView)
        {
            FileFilterCollection filter = null;

            if (layer.Table.GeometryType != GeometryType.Polygon)
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
                            snake.ButtonClick += async (p1, p2) => await mapView.ZoomToGeometryAsync(GeometryEngine.Project(features[0].Geometry.Extent, SpatialReferences.WebMercator));

                            snake.ShowMessage("已导出到" + path);
                        }
                        break;

                    case ".csv":
                        await Csv.ImportAsync(path, layer);
                        SnakeBar.Show("导入CSV成功");
                        break;
                }
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导入失败");
            }
        }

        public async static Task ExportLayerAsync(LayerInfo layer, LayerCollection layers)
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
                                 await Package.ExportLayer2Async(path, layer, layers);
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

        public async static Task ImportPackageAsync(MapLayerCollection layers)
        {
            bool ok = true;
            string path = FileSystemDialog.GetOpenFile(new FileFilterCollection()
                   .Add("mbmpkg地图画板包", "mbmpkg"));

            if (path != null)
            {
                try
                {
                    if (Config.Instance.BackupWhenReplace)
                    {
                        await Package.BackupAsync(layers, Config.Instance.MaxBackupCount);
                    }
                    await Package.ImportMapAsync(path, layers, true);
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
                        SnakeBar.Show(App.Current.MainWindow, "导入成功");
                    }
                }
            }
            return;
        }

        /// <summary>
        /// 显示对话框导入
        /// </summary>
        /// <returns>返回是否需要通知刷新Style</returns>
        public async static Task AddLayerAsync(MapLayerCollection layers)
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
                            await Package.ImportMapAsync(path, layers, false);
                            return;

                        case ".mblpkg":
                            await Package.ImportLayerAsync(path, layers);
                            return;

                        case ".gpx":
                            await ImportGpxAsync(new[] { path }, layers.Selected, layers);
                            break;

                        case ".shp":
                            await Shapefile.ImportAsync(path, layers);
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
        public static async Task ExportMapAsync(MapView mapView, MapLayerCollection layers)
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
                                await Package.ExportMap2Async(path, layers);
                                break;

                            case ".png":
                                await SaveImageAsync(path, mapView);
                                break;

                            case ".zip":
                                await MobileGISToolBox.ExportMapAsync(path, layers);
                                break;

                            case ".kmz":
                                await Kml.ExportAsync(path, layers);
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
        private static async Task SaveImageAsync(string path, MapView mapView)
        {
            RuntimeImage image = await mapView.ExportImageAsync();
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

        private static async Task ImportGpxAsync(string[] files, LayerInfo layer, MapLayerCollection layers)
        {
            List<DialogItem> items = new List<DialogItem>()
                {
                       new DialogItem("使用GPX工具箱打开","使用GPX工具箱打开该轨迹",()=>new GpxToolbox.UI.MainWindow(files).Show()),
                        new DialogItem("导入到新图层（线）","每一个文件将会生成一条线",async()=>await Gpx.ImportAllToNewLayerAsync(files,Gpx.GpxImportType.Line,layers)),
                        new DialogItem("导入到新图层（点）","生成所有文件的轨迹点",async()=>await Gpx.ImportAllToNewLayerAsync(files,Gpx.GpxImportType.Point,layers)),
                };
            if (layer != null)
            {
                if (layer.Table.GeometryType == GeometryType.Point || layer.Table.GeometryType == GeometryType.Polyline)
                {
                    items.Add(new DialogItem("导入到当前图层", "将轨迹导入到当前图层", async () => await Gpx.ImportToLayersAsync(files, layer)));
                }
            }
            await CommonDialog.ShowSelectItemDialogAsync("选择打开多个GPX文件的方式", items);
        }

        public async static Task DropFoldersAsync(string[] folders, MapLayerCollection layers)
        {
            int index = await CommonDialog.ShowSelectItemDialogAsync("请选择需要导入的内容", new DialogItem[]
            {
                new DialogItem("照片位置","根据照片EXIF信息的经纬度，生成点图层"),
            });
            switch (index)
            {
                case 0:
                    List<string> files = new List<string>();
                    string[] extensions = { ".jpg" };
                    await Task.Run(() =>
                    {
                        foreach (var folder in folders)
                        {
                            files.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(file => extensions.Contains(Path.GetExtension(file))));
                        }
                    });
                    await Photo.ImportImageLocation(files, layers);

                    break;

                default:
                    break;
            }
        }

        public async static Task DropFilesAsync(string[] files, MapLayerCollection layers)
        {
            if (files.Count(p => p.EndsWith(".gpx")) == files.Length)
            {
                await ImportGpxAsync(files, layers.Selected, layers);
            }
            else if (files.Count(p => p.EndsWith(".mbmpkg")) == files.Length && files.Length == 1)
            {
                if (await CommonDialog.ShowYesNoDialogAsync("是否覆盖当前所有样式？") == true)
                {
                    Package.ImportMapAsync(files[0], layers, true);
                }
                else
                {
                    Package.ImportMapAsync(files[0], layers, false);
                }
            }
            else if (files.Count(p => p.EndsWith(".mblpkg")) == files.Length)
            {
                if (await CommonDialog.ShowYesNoDialogAsync("是否导入图层？") == true)
                {
                    foreach (var file in files)
                    {
                        await Package.ImportLayerAsync(file, layers);
                    }
                }
            }
            else if (files.Count(p => p.EndsWith(".csv")) == files.Length)
            {
                if (await CommonDialog.ShowYesNoDialogAsync("是否导入CSV文件？") == true)
                {
                    foreach (var file in files)
                    {
                        await Csv.ImportAsync(file, layers.Selected);
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