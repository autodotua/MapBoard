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
using Microsoft.WindowsAPICodePack.Dialogs;
using FzLib.WPF;
using FzLib;

namespace MapBoard.UI
{
    public static class IOUtility
    {
        private static async Task ShowException(Exception ex, string message)
        {
            App.Log.Error(message, ex);
            if (ex is ItemsOperationException e)
            {
                await ItemsOperaionErrorsDialog.TryShowErrorsAsync(message, e.Errors);
            }
            else
            {
                await CommonDialog.ShowErrorDialogAsync(ex, message);
            }
        }
        public static FileFilterCollection AddIf(this FileFilterCollection filter, bool b, string display, params string[] extensions)
        {
            if (b)
            {
                return filter.Add(display, extensions);
            }
            return filter;
        }

        public static string GetImportFeaturePath(ImportLayerType type, Window parentWindow)
        {
            return new FileFilterCollection()
                .AddIf(type == ImportLayerType.Gpx, "GPS轨迹文件", "gpx")
                .AddIf(type == ImportLayerType.Csv, "CSV表格", "csv")
                .CreateOpenFileDialog()
                .SetParent(parentWindow)
                .GetFilePath();
        }

        public static async Task ImportFeatureAsync(Window owner, string path, IEditableLayerInfo layer, MainMapView mapView, ImportLayerType type)
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
                ShowException(ex, "导入失败");
            }
        }

        public static string GetExportLayerPath(ILayerInfo layer, ExportLayerType type, Window parentWindow)
        {
            if ((int)type <= (int)ExportLayerType.GeoJSON)
            {
                return new FileFilterCollection()
                    .AddIf(type == ExportLayerType.LayerPackge, "地图画板图层包", "mblpkg")
                    .AddIf(type == ExportLayerType.LayerPackgeRebuild, "图层包", "mblpkg")
                    .AddIf(type == ExportLayerType.GISToolBoxZip, "GIS工具箱图层包", "zip")
                    .AddIf(type == ExportLayerType.KML, "KML打包文件", "kmz")
                    .AddIf(type == ExportLayerType.GeoJSON, "GeoJSON文件", "geojson")
                    .CreateSaveFileDialog()
                    .SetDefault(layer.Name)
                    .SetParent(parentWindow)
                    .GetFilePath();
            }
            else
            {
                return new CommonOpenFileDialog()
                      .SetParent(parentWindow)
                      .GetFolderPath();
            }
        }

        public static async Task ExportLayerAsync(Window owner, string path, IMapLayerInfo layer, MapLayerCollection layers, ExportLayerType type)
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
                        if (layer is ShapefileMapLayerInfo s)
                        {
                            await MobileGISToolBox.ExportLayerAsync(path, s);
                        }
                        else
                        {
                            throw new NotSupportedException("非Shapefile图层不支持导出为GIS工具箱压缩包");
                        }
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
                ShowExportedSnackbarAndClickToOpenFolder(path, App.Current.MainWindow);
            }
            catch (Exception ex)
            {
                ShowException(ex, "导出失败");
            }
        }

        public static string GetImportMapPath(ImportMapType type, Window parentWindow)
        {
            return new FileFilterCollection()
                .AddIf(type == ImportMapType.MapPackageOverwrite, "地图画板地图包", "mbmpkg")
                .AddIf(type == ImportMapType.MapPackgeAppend, "地图画板地图包", "mbmpkg")
                .AddIf(type == ImportMapType.LayerPackge, "mblpkg地图画板图层包", "mblpkg")
                .AddIf(type == ImportMapType.Gpx, "GPS轨迹文件", "gpx")
                .AddIf(type == ImportMapType.Shapefile, "Shapefile", "shp")
                .AddIf(type == ImportMapType.CSV, "CSV表格", "csv")
                .CreateOpenFileDialog()
                .SetParent(parentWindow)
                .GetFilePath();
        }

        public static async Task ImportMapAsync(Window owner, string path, MapLayerCollection layers, ImportMapType type, ProgressRingOverlayArgs args)
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
                                App.Log.Error("备份失败", ex);
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
                ShowException(ex, "导入失败");
            }
        }

        public static string GetExportMapPath(ExportMapType type, Window parentWindow)
        {
            if (type is ExportMapType.OpenLayers)
            {
                return new CommonOpenFileDialog()
                    .SetParent(parentWindow)
                    .GetFolderPath();
            }
            else
            {
                return new FileFilterCollection()
                .AddIf(type == ExportMapType.MapPackage, "地图画板地图包", "mbmpkg")
                .AddIf(type == ExportMapType.MapPackageRebuild, "地图画板地图包", "mbmpkg")
                .AddIf(type == ExportMapType.GISToolBoxZip, "GIS工具箱图层包", "zip")
                .AddIf(type == ExportMapType.KML, "KML打包文件", "kmz")
                .AddIf(type == ExportMapType.Screenshot, "截图", "png")
                .CreateSaveFileDialog()
                .SetDefault("地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"))
                .SetParent(parentWindow)
                .GetFilePath();
            }
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

                ShowExportedSnackbarAndClickToOpenFolder(path, App.Current.MainWindow);
            }
            catch (Exception ex)
            {
                ShowException(ex, "导出失败");
            }
        }

        private static async Task ImportGpxAsync(IEnumerable<string> files, IMapLayerInfo layer, MapLayerCollection layers)
        {
            List<SelectDialogItem> items = new List<SelectDialogItem>()
                {
                       new SelectDialogItem("使用GPX工具箱打开", "使用GPX工具箱打开该轨迹"),
                        new SelectDialogItem("导入到新图层（线）","每一个文件将会生成一条线"),
                        new SelectDialogItem("导入到新图层（点）","生成所有文件的轨迹点"),
                };
            if (layer != null
                && layer is IEditableLayerInfo
                && layer.GeometryType is GeometryType.Point or GeometryType.Polyline)
            {
                items.Add(new SelectDialogItem("导入到当前图层", "将轨迹导入到当前图层"));
            }
            int index = await CommonDialog.ShowSelectItemDialogAsync($"选择打开GPX文件的方式，共{files.Count()}个文件", items);
            switch (index)
            {
                case 0:
                    var win = new GpxToolbox.GpxWindow
                    {
                        LoadFiles = files.ToArray()
                    };
                    win.Show();
                    win.BringToFront();
                    break;
                case 1:
                    await Gps.ImportAllToNewLayerAsync(files, Gps.GpxImportType.Line, layers, Config.Instance.BasemapCoordinateSystem);
                    break;
                case 2:
                    await Gps.ImportAllToNewLayerAsync(files, Gps.GpxImportType.Point, layers, Config.Instance.BasemapCoordinateSystem);
                    break;
                case 3:
                    await Gps.ImportToLayersAsync(files, layer as IEditableLayerInfo, Config.Instance.BasemapCoordinateSystem);
                    break;

            }
        }

        private static async Task<List<string>> EnumerateFilesAsync(IEnumerable<string> folders, IEnumerable<string> extensions)
        {
            List<string> files = new List<string>();
            var exs = extensions
                .Select(p => p.StartsWith(".") ? p : $".{p}")
                .Select(p=>p.ToLower())
                .ToList();
            await Task.Run(() =>
            {
                foreach (var folder in folders)
                {
                    if (File.Exists(folder) 
                    && exs.Contains(Path.GetExtension(folder).ToLower()))
                    {
                        files.Add(folder);
                    }
                    else if (Directory.Exists(folder))
                    {
                        files.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                            .Where(p => exs.Contains(Path.GetExtension(p).ToLower())));
                    }
                }
            });
            return files;
        }
        public static async Task DropFoldersAsync(string[] folders, MapLayerCollection layers)
        {
            int index = await CommonDialog.ShowSelectItemDialogAsync("请选择目录中的文件类型", new SelectDialogItem[]
            {
                new SelectDialogItem("照片位置","根据照片EXIF信息的经纬度，生成点图层"),
                new SelectDialogItem("GPS轨迹文件","作为GPX文件导入"),
            });
            if (index >= 0)
            {
                try
                {
                    List<string> files = null;
                    switch (index)
                    {
                        case 0:
                            string[] extensions = { ".jpg",".jpeg",".heif",".heic",".dng" };
                            files = await EnumerateFilesAsync(folders, extensions);
                            await Photo.ImportImageLocation(files, layers);

                            break;
                        case 1:
                            files = await EnumerateFilesAsync(folders, new string[] { ".gpx" });
                            await ImportGpxAsync(files, layers.Selected, layers);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ShowException(ex, "导入失败");
                }
            }
        }

        public static async Task DropFilesAsync(string[] files, MapLayerCollection layers)
        {
            try
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
                else if (files.Count(p => p.EndsWith(".csv")) == files.Length && layers.Selected is IEditableLayerInfo w2)
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
            catch (Exception ex)
            {
                ShowException(ex, "导入失败");
            }
        }

        public static void ShowExportedSnackbarAndClickToOpenFolder(string path, Window owner)
        {
            SnakeBar snake = new SnakeBar(owner)
            {
                ShowButton = true,
                ButtonContent = "查看"
            };
            snake.ButtonClick += async (p1, p2) => await TryOpenFolderAsync(path);

            snake.ShowMessage("导出成功");
        }

        /// <summary>
        /// 使用Shell打开文件，文件夹或Url等。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task TryOpenInShellAsync(string path)
        {
            try
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo(GetRealPath(path))
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
            catch (Exception ex)
            {
                App.Log.Error("打开失败", ex);
                await CommonDialog.ShowErrorDialogAsync(ex, "打开失败");
            }
        }

        /// <summary>
        /// 若输入为目录，直接打开；若输入为文件，则打开目录并选中文件。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task TryOpenFolderAsync(string path)
        {
            try
            {
                var p = '"' + GetRealPath(path) + '"';
                if (Directory.Exists(path))
                {
                    new Process()
                    {
                        StartInfo = new ProcessStartInfo("explorer.exe", p)
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                }
                if (File.Exists(path))
                {
                    new Process()
                    {
                        StartInfo = new ProcessStartInfo("explorer.exe", $"/select,{p}")
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("打开文件夹失败", ex);
                await CommonDialog.ShowErrorDialogAsync("文件或目录不存在", ex.Message);
            }
        }

        /// <summary>
        /// 在MSIX下运行时，程序数据目录的位置被重定向。在程序内访问时不需要修改，但通过外部程序调用时，需要获取真实的路径。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetRealPath(string path)
        {
            path = Path.GetFullPath(path);
            string oldPath = path;
            if (File.Exists(path) || Directory.Exists(path))
            {
                return path;
            }
            var helper = new DesktopBridge.Helpers();
            if (helper.IsRunningAsUwp())
            {
                string virtualAppData = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
                string local = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Parent.FullName;
                if (path.Contains(local))
                {
                    path = path.Replace(local, virtualAppData);
                }
            }
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException(path, new FileNotFoundException(oldPath));
            }
            return path;
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
        GISToolBoxNet = 4,
        KML = 5,
        Screenshot = 6,
        OpenLayers = 7,
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
        GeoJSON = 5,
        OpenLayers = 6,
    }
}