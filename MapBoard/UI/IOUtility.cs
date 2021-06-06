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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MapBoard.Main.UI.Map.Model;

namespace MapBoard.Main.UI
{
    public static class IOUtility
    {
        public async static Task ImportFeatureAsync(MapLayerInfo layer, ArcMapView mapView, ImportLayerType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ImportLayerType.Gpx => filter.Add("GPS轨迹文件", "gpx"),
                ImportLayerType.Csv => filter.Add("CSV表格", "csv"),
                _ => throw new ArgumentOutOfRangeException()
            };
            string path = FileSystemDialog.GetOpenFile(filter);
            if (path != null)
            {
                try
                {
                    IReadOnlyList<Feature> features = null;
                    switch (type)
                    {
                        case ImportLayerType.Gpx:
                            features = await Gpx.ImportToLayerAsync(path, layer);
                            break;

                        case ImportLayerType.Csv:
                            features = await Csv.ImportAsync(path, layer);
                            break;

                        default:
                            break;
                    }
                    SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner);
                    snake.ShowButton = true;
                    snake.ButtonContent = "查看";
                    snake.ButtonClick += async (p1, p2) =>
                    {
                        var geom = GeometryEngine.CombineExtents(features.Select(p => p.Geometry));
                        await mapView.ZoomToGeometryAsync(geom);
                    };
                    snake.ShowMessage("导入成功");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导入失败");
                }
            }
        }

        public async static Task ExportLayerAsync(MapLayerInfo layer, MapLayerCollection layers, ExportLayerType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ExportLayerType.LayerPackge => filter.Add("mblpkg地图画板包", "mblpkg"),
                ExportLayerType.GISToolBoxZip => filter.Add("GIS工具箱图层包", "zip"),
                ExportLayerType.KML => filter.Add("KML打包文件", "kmz"),
                ExportLayerType.GeoJSON => filter.Add("GeoJSON文件", "geojson"),
                _ => throw new ArgumentOutOfRangeException()
            };
            string path = FileSystemDialog.GetSaveFile(filter, true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (path != null)
            {
                try
                {
                    switch (type)
                    {
                        case ExportLayerType.LayerPackge:
                            await Package.ExportLayer2Async(path, layer, layers);
                            break;

                        case ExportLayerType.GISToolBoxZip:
                            await MobileGISToolBox.ExportLayerAsync(path, layer);
                            break;

                        case ExportLayerType.KML:
                            await Kml.ExportAsync(path, layer);
                            break;

                        case ExportLayerType.GeoJSON:
                            await GeoJson.ExportAsync(path, layer);
                            break;

                        default:
                            break;
                    }
                    SnakeBar.Show(App.Current.MainWindow, "导出成功");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
                }
            }
        }

        public async static Task ImportMapAsync(MapLayerCollection layers, ImportMapType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ImportMapType.MapPackageOverwrite or ImportMapType.MapPackgeAppend
                => filter.Add("mbmpkg地图画板包", "mbmpkg"),
                ImportMapType.LayerPackge => filter.Add("mblpkg地图画板图层包", "mblpkg"),
                ImportMapType.Gpx => filter.Add("GPS轨迹文件", "gpx"),
                ImportMapType.Shapefile => filter.Add("Shapefile", "shp"),
                _ => throw new ArgumentOutOfRangeException()
            };
            string path = FileSystemDialog.GetOpenFile(filter);
            if (path != null)
            {
                try
                {
                    switch (type)
                    {
                        case ImportMapType.MapPackageOverwrite:
                            if (Config.Instance.BackupWhenReplace)
                            {
                                await Package.BackupAsync(layers, Config.Instance.MaxBackupCount);
                            }
                            await Package.ImportMapAsync(path, layers, true);
                            break;

                        case ImportMapType.MapPackgeAppend:
                            await Package.ImportMapAsync(path, layers, false);
                            break;

                        case ImportMapType.LayerPackge:
                            await Package.ImportLayerAsync(path, layers);
                            break;

                        case ImportMapType.Gpx:
                            await ImportGpxAsync(new[] { path }, layers.Selected, layers);
                            break;

                        case ImportMapType.Shapefile:
                            await Shapefile.ImportAsync(path, layers);
                            break;

                        default:
                            break;
                    }
                    SnakeBar.Show(App.Current.MainWindow, "导入成功");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导入失败");
                }
            }
        }

        public static async Task ExportMapAsync(MapView mapView, MapLayerCollection layers, ExportMapType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ExportMapType.MapPackage
                => filter.Add("mbmpkg地图画板包", "mbmpkg"),
                ExportMapType.GISToolBoxZip => filter.Add("GIS工具箱图层包", "zip"),
                ExportMapType.KML => filter.Add("KML打包文件", "kmz"),
                ExportMapType.Screenshot => filter.Add("截图", "png"),
                _ => throw new ArgumentOutOfRangeException()
            };
            string path = FileSystemDialog.GetSaveFile(filter, true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (path != null)
            {
                try
                {
                    switch (type)
                    {
                        case ExportMapType.MapPackage:
                            await Package.ExportMap2Async(path, layers);
                            break;

                        case ExportMapType.GISToolBoxZip:
                            await MobileGISToolBox.ExportMapAsync(path, layers);
                            break;

                        case ExportMapType.KML:
                            await Kml.ExportAsync(path, layers.Cast<MapLayerInfo>());
                            break;

                        case ExportMapType.Screenshot:
                            await SaveImageAsync(path, mapView);
                            break;

                        default:
                            break;
                    }
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

        private static async Task ImportGpxAsync(string[] files, MapLayerInfo layer, MapLayerCollection layers)
        {
            List<DialogItem> items = new List<DialogItem>()
                {
                       new DialogItem("使用GPX工具箱打开","使用GPX工具箱打开该轨迹",()=>new GpxToolbox.UI.MainWindow(files).Show()),
                        new DialogItem("导入到新图层（线）","每一个文件将会生成一条线",async()=>await Gpx.ImportAllToNewLayerAsync(files,Gpx.GpxImportType.Line,layers)),
                        new DialogItem("导入到新图层（点）","生成所有文件的轨迹点",async()=>await Gpx.ImportAllToNewLayerAsync(files,Gpx.GpxImportType.Point,layers)),
                };
            if (layer != null)
            {
                if (layer.GeometryType is GeometryType.Point or GeometryType.Polyline)
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

        public static void OpenFileOrFolder(string path)
        {
            new Process()
            {
                StartInfo = new ProcessStartInfo(Path.GetFullPath(path))
                {
                    UseShellExecute = true
                }
            }.Start();
        }
    }

    public enum ImportMapType
    {
        MapPackageOverwrite = 1,
        MapPackgeAppend = 2,
        LayerPackge = 3,
        Gpx = 4,
        Shapefile = 5
    }

    public enum ExportMapType
    {
        MapPackage = 1,
        GISToolBoxZip = 2,
        KML = 3,
        Screenshot = 4
    }

    public enum ImportLayerType
    {
        Csv = 1,
        Gpx = 2
    }

    public enum ExportLayerType
    {
        LayerPackge = 1,
        GISToolBoxZip = 2,
        KML = 3,
        GeoJSON = 4
    }
}