using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.WPF.Dialog;
using MapBoard.IO;
using MapBoard.Model;
using MapBoard.UI;
using MapBoard.Mapping;
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
using ModernWpf.FzExtension;
using MapBoard.Mapping.Model;
using MapBoard.UI.Dialog;
using System.Drawing.Imaging;
using Microsoft.WindowsAPICodePack.FzExtension;

namespace MapBoard.UI
{
    public static class IOUtility
    {
        public static string GetImportFeaturePath(ImportLayerType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ImportLayerType.Gpx => filter.Add("GPS轨迹文件", "gpx"),
                ImportLayerType.Csv => filter.Add("CSV表格", "csv"),
                _ => throw new ArgumentOutOfRangeException()
            };
            return FileSystemDialog.GetOpenFile(filter);
        }

        public async static Task ImportFeatureAsync(Window owner, string path, IWriteableLayerInfo layer, MainMapView mapView, ImportLayerType type)
        {
            Debug.Assert(path != null);

            try
            {
                IReadOnlyList<Feature> features = null;
                switch (type)
                {
                    case ImportLayerType.Gpx:
                        features = await Gps.ImportToLayerAsync(path, layer, Config.Instance.BasemapCoordinateSystem);
                        break;

                    case ImportLayerType.Csv:
                        features = await Csv.ImportAsync(path, layer);
                        break;

                    default:
                        break;
                }
                SnakeBar snake = new SnakeBar(owner);
                snake.ShowButton = true;
                snake.ButtonContent = "查看";
                snake.ButtonClick += async (p1, p2) =>
                {
                    var geom = GeometryEngine.CombineExtents(features.Select(p => p.Geometry));
                    await mapView.ZoomToGeometryAsync(geom);
                };
                mapView.Layers.Save();
                snake.ShowMessage("导入成功");
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导入失败");
            }
        }

        public static string GetExportLayerPath(ILayerInfo layer, ExportLayerType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ExportLayerType.LayerPackge => filter.Add("mblpkg地图画板包", "mblpkg"),
                ExportLayerType.LayerPackgeRebuild => filter.Add("mblpkg地图画板包", "mblpkg"),
                ExportLayerType.GISToolBoxZip => filter.Add("GIS工具箱图层包", "zip"),
                ExportLayerType.KML => filter.Add("KML打包文件", "kmz"),
                ExportLayerType.GeoJSON => filter.Add("GeoJSON文件", "geojson"),
                _ => throw new ArgumentOutOfRangeException()
            };
            return FileSystemDialog.GetSaveFile(filter, true, layer.Name);
        }

        public async static Task ExportLayerAsync(Window owner, string path, IMapLayerInfo layer, MapLayerCollection layers, ExportLayerType type)
        {
            Debug.Assert(path != null);
            try
            {
                switch (type)
                {
                    case ExportLayerType.LayerPackge:
                        await Package.ExportLayerAsync(path, layer, Config.Instance.CopyShpFileWhenExport);
                        break;

                    case ExportLayerType.LayerPackgeRebuild:
                        await Package.ExportLayerAsync(path, layer, false);
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
                SnakeBar.Show(owner, "导出成功");
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
            }
        }

        public static string GetImportMapPath(ImportMapType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ImportMapType.MapPackageOverwrite or ImportMapType.MapPackgeAppend
                => filter.Add("mbmpkg地图画板包", "mbmpkg"),
                ImportMapType.LayerPackge => filter.Add("mblpkg地图画板图层包", "mblpkg"),
                ImportMapType.Gpx => filter.Add("GPS轨迹文件", "gpx"),
                ImportMapType.Shapefile => filter.Add("Shapefile", "shp"),
                ImportMapType.CSV => filter.Add("CSV表格", "csv"),
                _ => throw new ArgumentOutOfRangeException()
            };
            return FileSystemDialog.GetOpenFile(filter);
        }

        public async static Task ImportMapAsync(Window owner, string path, MapLayerCollection layers, ImportMapType type, ProgressRingOverlayArgs args)
        {
            Debug.Assert(path != null);
            try
            {
                switch (type)
                {
                    case ImportMapType.MapPackageOverwrite:
                        args.SetMessage("正在备份当前地图");
                        if (Config.Instance.BackupWhenReplace)
                        {
                            try
                            {
                                await Package.BackupAsync(layers, Config.Instance.MaxBackupCount, Config.Instance.CopyShpFileWhenExport);
                            }
                            catch (Exception ex)
                            {
                                SnakeBar.ShowError("备份失败");
                            }
                        }
                        args.SetMessage("正在导入新的地图");
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

                    case ImportMapType.CSV:
                        var table = await Csv.ImportToDataTableAsync(path);
                        await new ImportTableDialog(layers, table, Path.GetFileNameWithoutExtension(path))
                            .ShowAsync();
                        break;

                    default:
                        break;
                }
                layers.Save();
                SnakeBar.Show(owner, "导入成功");
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导入失败");
            }
        }

        public static string GetExportMapPath(ExportMapType type)
        {
            FileFilterCollection filter = new FileFilterCollection();
            filter = type switch
            {
                ExportMapType.MapPackage or ExportMapType.MapPackageRebuild
                => filter.Add("mbmpkg地图画板包", "mbmpkg"),
                ExportMapType.GISToolBoxZip => filter.Add("GIS工具箱图层包", "zip"),
                ExportMapType.KML => filter.Add("KML打包文件", "kmz"),
                ExportMapType.Screenshot => filter.Add("截图", "png"),
                _ => throw new ArgumentOutOfRangeException()
            };
            return FileSystemDialog.GetSaveFile(filter, true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        public static async Task ExportMapAsync(Window owner, string path, MapView mapView, MapLayerCollection layers, ExportMapType type)
        {
            Debug.Assert(path != null);
            try
            {
                switch (type)
                {
                    case ExportMapType.MapPackage:
                        await Package.ExportMapAsync(path, layers, Config.Instance.CopyShpFileWhenExport);
                        break;

                    case ExportMapType.MapPackageRebuild:
                        await Package.ExportMapAsync(path, layers, false);
                        break;

                    case ExportMapType.GISToolBoxZip:
                        await MobileGISToolBox.ExportMapAsync(path, layers);
                        break;

                    case ExportMapType.KML:
                        await Kml.ExportAsync(path, layers.Cast<MapLayerInfo>());
                        break;

                    case ExportMapType.Screenshot:
                        await mapView.ExportImageAsync(path, ImageFormat.Png, GeoViewHelper.GetWatermarkThickness());

                        break;

                    default:
                        break;
                }
                SnakeBar.Show(owner, "导出成功");
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
            }
        }

        private static async Task ImportGpxAsync(string[] files, IMapLayerInfo layer, MapLayerCollection layers)
        {
            List<SelectDialogItem> items = new List<SelectDialogItem>()
                {
                       new SelectDialogItem("使用GPX工具箱打开","使用GPX工具箱打开该轨迹",()=>new GpxToolbox.GpxWindow(files).Show()),
                        new SelectDialogItem("导入到新图层（线）","每一个文件将会生成一条线",async()=>await Gps.ImportAllToNewLayerAsync(files,Gps.GpxImportType.Line,layers,Config.Instance.BasemapCoordinateSystem)),
                        new SelectDialogItem("导入到新图层（点）","生成所有文件的轨迹点",async()=>await Gps.ImportAllToNewLayerAsync(files,Gps.GpxImportType.Point,layers,Config.Instance.BasemapCoordinateSystem)),
                };
            if (layer != null && layer is IWriteableLayerInfo w)
            {
                if (layer.GeometryType is GeometryType.Point or GeometryType.Polyline)
                {
                    items.Add(new SelectDialogItem("导入到当前图层", "将轨迹导入到当前图层", async () => await Gps.ImportToLayersAsync(files, w, Config.Instance.BasemapCoordinateSystem)));
                }
            }
            await CommonDialog.ShowSelectItemDialogAsync("选择打开多个GPX文件的方式", items);
        }

        public async static Task DropFoldersAsync(string[] folders, MapLayerCollection layers)
        {
            int index = await CommonDialog.ShowSelectItemDialogAsync("请选择需要导入的内容", new SelectDialogItem[]
            {
                new SelectDialogItem("照片位置","根据照片EXIF信息的经纬度，生成点图层"),
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
                    await Package.ImportMapAsync(files[0], layers, true);
                }
                else
                {
                    await Package.ImportMapAsync(files[0], layers, false);
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
            else if (files.Count(p => p.EndsWith(".csv")) == files.Length && layers.Selected is IWriteableLayerInfo w2)
            {
                if (await CommonDialog.ShowYesNoDialogAsync("是否导入CSV文件？") == true)
                {
                    foreach (var file in files)
                    {
                        await Csv.ImportAsync(file, w2);
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
        Shapefile = 5,
        CSV = 6
    }

    public enum ExportMapType
    {
        MapPackage = 1,
        MapPackageRebuild = 2,
        GISToolBoxZip = 3,
        KML = 4,
        Screenshot = 5
    }

    public enum ImportLayerType
    {
        Csv = 1,
        Gpx = 2
    }

    public enum ExportLayerType
    {
        LayerPackge = 1,
        LayerPackgeRebuild = 2,
        GISToolBoxZip = 3,
        KML = 4,
        GeoJSON = 5
    }
}