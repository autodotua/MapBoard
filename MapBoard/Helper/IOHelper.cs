using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.UI.Dialog;
using MapBoard.Main.IO;
using MapBoard.Main.Layer;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Map;
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

namespace MapBoard.Main.Helper
{
    public static class IOHelper
    {
        public async static Task ImportFeature()
        {
            LayerInfo style = LayerCollection.Instance.Selected;
            string path = null;
            if (style.Type != GeometryType.Polygon)
            {
                path = FileSystemDialog.GetOpenFile(new List<(string, string)>()
                {
                    ("支持的格式", "csv,gpx"),
                    ("CSV表格", "csv"),
                    ("GPS轨迹文件","gpx") ,
                }, true);
            }
            else
            {
                path = FileSystemDialog.GetOpenFile(new List<(string, string)>()
                {
                    ("CSV表格", "csv"),
                }, true);
            }
            if (path != null)
            {
                try
                {
                    switch (Path.GetExtension(path))
                    {
                        case ".csv":
                            await Csv.Import(path);
                            SnakeBar.Show("导入CSV成功");
                            break;

                        case ".gpx":
                            var features = await Gpx.ImportToCurrentLayer(path);
                            if (features.Length > 1)
                            {

                                SnakeBar.Show("导入GPX成功");
                            }
                            else
                            {
                                SnakeBar snake = new SnakeBar(SnakeBar.DefaultOwner.Owner);
                                snake.ShowButton = true;
                                snake.ButtonContent = "查看";
                                snake.ButtonClick += (p1, p2) => ArcMapView.Instance.SetViewpointGeometryAsync(GeometryEngine.Project(features[0].Geometry.Extent, SpatialReferences.WebMercator));

                                snake.ShowMessage("已导出到" + path);
                            }
                            break;

                        default:
                            throw new Exception("未知文件类型");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(ex, "导入失败");
                }
            }
        }

        /// <summary>
        /// 显示对话框导入
        /// </summary>
        /// <returns>返回是否需要通知刷新Style</returns>
        public async static Task ImportLayer()
        {
            bool ok = true;
            string path = FileSystemDialog.GetOpenFile(new List<(string, string)>()
            {
                ("支持的格式", "mbmpkg,mblpkg,gpx,shp"),
                ("mbmpkg地图画板包", "mbmpkg"),
                ("mblpkg地图画板图层包", "mblpkg"),
                ("GPS轨迹文件","gpx") ,
                ("Shapefile文件","shp") ,
            }
            , true);
            if (path != null)
            {
                try
                {
                    switch (Path.GetExtension(path))
                    {
                        case ".mbmpkg":
                            Package.ImportMap(path);
                            return  ;

                        case ".mblpkg":
                            Package.ImportLayer(path);
                            return ;

                        case ".gpx":
                            string result= TaskDialog.ShowWithCommandLinks("请选择转换类型", "正在准备导入GPS轨迹文件",
                           new (string, string, Action)[] {
                                ("点","每一个轨迹点分别加入到新的样式中",null),
                                ("一条线","按时间顺序将轨迹点相连，形成一条线",null),
                                ("多条线","按时间顺序将每两个轨迹点相连，形成n-1条线",null) }, cancelable: true);
                            switch(result)
                            {
                                case "点":
                                    await Gpx.ImportToNewStyle(path, Gpx.Type.Point);
                                    break;
                                case "一条线":
                                    await Gpx.ImportToNewStyle(path, Gpx.Type.OneLine);
                                    break;
                                case "多条线":
                                    await Gpx.ImportToNewStyle(path, Gpx.Type.MultiLine);
                                    break;
                                case null:
                                    ok = false;
                                    break;
                            }
                            break;
                        case ".shp":
                            await Shapefile.Import(path);
                            return  ;

                        default:
                            throw new Exception("未知文件类型");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(ex, "导入失败");
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
        public static async Task ExportLayer()
        {
            string path = FileSystemDialog.GetSaveFile(new List<(string, string)>() {
                ("mbmpkg地图画板包", "mbmpkg"),
                ("截图", "png")
            },
                false, true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (path != null)
            {
                try
                {
                    SnakeBar.Show("正在导出");
                    switch (Path.GetExtension(path))
                    {
                        case ".mbmpkg":
                            await Package.ExportMap2(path);
                            //Package.ExportMap(path);
                            break;

                        case ".png":
                            await SaveImage(path);
                            break;
                    }
                    SnakeBar.Show("导出成功");
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(ex, "导出失败");
                }
            }
        }
        /// <summary>
        /// 保存截图
        /// </summary>
        /// <param name="path">保存地址</param>
        /// <returns></returns>
        private static async Task SaveImage(string path)
        {
            //ArcMapView.Instance.Width = 10000;
            //ArcMapView.Instance.Height = 10000;
            //await Task.Delay(1000);
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

        public async static Task DropFiles(string[] files)
        {
            if (files.Count(p => p.EndsWith(".gpx")) == files.Length)
            {
                if (files.Length > 1)
                {
                    //if (TaskDialog.ShowWithYesNoButtons("通过GPX工具箱打开？", "打开GPX文件", icon: Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.Information) == true)
                    //{
                        TaskDialog.ShowWithCommandLinks("选择打开多个GPX文件的方式", "打开GPX文件", new (string, string, Action)[]{
                    ("使用GPX工具箱打开","使用GPX工具箱打开该轨迹",()=>new GpxToolbox.MainWindow(files).Show()),
                    ("导入到新样式","每一个文件将会生成一条线",async()=>await Gpx.ImportAllToNewStyle(files)),
                }, icon: Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.Information, cancelable: true);

                    //}
                }
                else
                {
                    TaskDialog.ShowWithCommandLinks("选择打开GPX文件的方式", "打开GPX文件", new (string, string, Action)[]{
                    ("使用GPX工具箱打开","使用GPX工具箱打开该轨迹",()=>new GpxToolbox.MainWindow(files).Show()),
                    ("导入为点","每一个轨迹点分别加入到新的样式中",async()=>await Gpx.ImportToNewStyle(files[0],Gpx.Type.Point)),
                    ("导入为一条线","按时间顺序将轨迹点相连，形成一条线",async()=>await Gpx.ImportToNewStyle(files[0],Gpx.Type.OneLine)),
                    ("导入为多条线","按时间顺序将每两个轨迹点相连，形成n-1条线",async()=>await Gpx.ImportToNewStyle(files[0],Gpx.Type.MultiLine)),
                }, icon: Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.Information, cancelable: true);
                }
            }
            else if (files.Count(p => p.EndsWith(".mbmpkg")) == files.Length && files.Length == 1)
            {
                if (TaskDialog.ShowWithYesNoButtons("是否覆盖当前所有样式？", "打开Mapboard Map Package文件", icon: Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.Information) == true)
                {
                    Package.ImportMap(files[0]);
                }
            }
            else if (files.Count(p => p.EndsWith(".mblpkg")) == files.Length)
            {
                if (TaskDialog.ShowWithYesNoButtons("是否导入图层？", "打开Mapboard Layer Package文件", icon: Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.Information) == true)
                {
                    foreach (var file in files)
                    {
                        Package.ImportLayer(file);
                        await Task.Delay(500);
                    }
                }
            }
            else if (files.Count(p => p.EndsWith(".csv")) == files.Length)
            {
                if (TaskDialog.ShowWithYesNoButtons("是否导入CSV文件？", "打开CSV", icon: Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.Information) == true)
                {
                    foreach (var file in files)
                    {
                        await IO.Csv.Import(file);
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
