using Esri.ArcGISRuntime.UI;
using FzLib.Control.Dialog;
using MapBoard.UI;
using MapBoard.UI.Map;
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

namespace MapBoard.IO
{
    public static class IOHelper
    {
        /// <summary>
        /// 显示对话框导入
        /// </summary>
        /// <returns>返回是否需要通知刷新Style</returns>
        public async static Task<bool> Import()
        {
            bool ok = true;
            string path = FileSystemDialog.GetOpenFile(new List<(string, string)>()
            { ("mbmpkg地图画板包", "mbmpkg"),
                ("mblpkg地图画板图层包", "mblpkg"),
                ("GPS轨迹文件","gpx") ,
                ("Shapefile文件","shp") ,
            }
            , true, true);
            if (path != null)
            {
                try
                {
                    switch (Path.GetExtension(path))
                    {
                        case ".mbmpkg":
                            Mbpkg.ImportMap(path);
                            return true;

                        case ".mblpkg":
                            Mbpkg.ImportLayer(path);
                            return false;

                        case ".gpx":
                            TaskDialog.ShowWithCommandLinks("请选择转换类型", "正在准备导入GPS轨迹文件",
                           new (string, string, Action)[] {
                                ("点","每一个轨迹点分别加入到新的样式中",()=>Gpx.Import(path,Gpx.Type.Point)),
                                ("一条线","按时间顺序将轨迹点相连，形成一条线",()=>Gpx.Import(path,Gpx.Type.OneLine)),
                                ("多条线","按时间顺序将每两个轨迹点相连，形成n-1条线",()=>Gpx.Import(path,Gpx.Type.MultiLine)),
                           });
                            return false;

                        case ".shp":
                            await Shapefile.Import(path);
                            return false;

                        default:
                            throw new Exception("未知文件类型");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(ex, "导入失败");
                    ok = false;
                    return false;
                }
                finally
                {
                    if (ok)
                    {
                        SnakeBar.Show("导入成功");
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 显示对话框导出
        /// </summary>
        /// <returns></returns>
        public static async Task Export()
        {
            string path = FileSystemDialog.GetSaveFile(new List<(string, string)>() {
                ("mbmpkg地图画板包", "mbmpkg"),  ("截图", "png")},
                false, true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (path != null)
            {
                try
                {
                    switch (Path.GetExtension(path))
                    {
                        case ".mbmpkg":

                            Mbpkg.ExportMap(path);
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
    }
}
