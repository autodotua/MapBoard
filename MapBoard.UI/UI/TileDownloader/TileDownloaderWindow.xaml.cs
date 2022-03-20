using Esri.ArcGISRuntime.Geometry;
using MapBoard.IO.Tile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ModernWpf.FzExtension.CommonDialog;
using FzLib;
using FzLib.WPF.Dialog;
using MapBoard.Util;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using Microsoft.WindowsAPICodePack.FzExtension;
using MapBoard.IO;
using System.Drawing.Imaging;

namespace MapBoard.UI.TileDownloader
{
    public enum DownloadStatus
    {
        Downloading,
        Paused,
        Stop,
        Pausing
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TileDownloaderWindow : MainWindowBase
    {
        private bool closing = false;

        private DownloadStatus currentDownloadStatus = DownloadStatus.Stop;

        /// <summary>
        /// 当前投影信息
        /// </summary>
        private ProjectInfo currentProject;

        /// <summary>
        /// 暂停时瓦片的序号
        /// </summary>
        private int lastIndex = 0;

        /// <summary>
        /// 暂停时最后一块瓦片
        /// </summary>
        private TileInfo lastTile = null;

        /// <summary>
        /// 保存的拼接完成后临时图片的位置
        /// </summary>
        private string savedImgPath = null;

        /// <summary>
        /// 是否有终止拼接的命令
        /// </summary>
        private bool stopStich = false;

        private bool waiting = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TileDownloaderWindow()
        {
            InitializeComponent();
            BindingOperations.EnableCollectionSynchronization(DownloadErrors, DownloadErrors);

            // txtUrl.Text = Config.Instance.Url;
            //SnakeBar.DefaultWindow = this;
            foreach (var i in Enumerable.Range(0, 21))
            {
                cbbLevel.Items.Add(i);
            }
            cbbLevel.SelectedIndex = 10;
            arcMap.ViewpointChanged += ArcMapViewpointChanged;
        }

        public Config Config => Config.Instance;
        public bool ControlsEnable { get; set; } = true;

        public DownloadInfo CurrentDownload { get; set; }

        public DownloadStatus CurrentDownloadStatus
        {
            get => currentDownloadStatus;
            set
            {
                currentDownloadStatus = value;
                switch (value)
                {
                    case DownloadStatus.Downloading:
                        taskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                        ControlsEnable = false;
                        break;

                    case DownloadStatus.Paused:
                    case DownloadStatus.Stop:
                        ControlsEnable = true;
                        taskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                        DownloadingProgressPercent = 0;
                        break;

                    case DownloadStatus.Pausing:
                        break;
                }
            }
        }

        public ObservableCollection<dynamic> DownloadErrors { get; } = new ObservableCollection<dynamic>();

        public double DownloadingProgressPercent { get; set; }

        public string DownloadingProgressStatus { get; set; } = "准备就绪";

        ///// <summary>
        ///// 是否正在下载
        ///// </summary>
        //private bool downloading = false;
        public IReadOnlyList<string> Formats { get; } = new List<string> { "jpg", "png", "bmp", "tiff" }.AsReadOnly();

        public string LastDownloadingStatus { get; set; } = "准备就绪";

        public string LastDownloadingTile { get; set; } = "还未下载";
        public bool ServerOn { get; set; }

        protected override async Task InitializeAsync()
        {
            await Task.Yield();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        private void arcMap_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (waiting)
            {
                return;
            }
            TileDownloaderMapView map = sender as TileDownloaderMapView;
            if (map.Map == null || map.Map.LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                return;
            }
            MapPoint point = map.ScreenToLocation(e.GetPosition(sender as IInputElement));

            point = GeometryEngine.Project(point, SpatialReferences.Wgs84) as MapPoint;
            int z = (int)(Math.Log(1e9 / map.MapScale, 2));
            (int x, int y) = TileLocation.GeoPointToTile(point, z);
            string l = Environment.NewLine;
            tbkTileIndex.Text = $"Z={z}{l}X={x}{l}Y={y}";
            waiting = true;
            Task.Delay(250).ContinueWith(p => waiting = false);
        }

        private void arcMap_SelectBoundaryComplete(object sender, EventArgs e)
        {
            downloadBoundary.SetDoubleValue(arcMap.Boundary.XMin, arcMap.Boundary.YMax, arcMap.Boundary.XMax, arcMap.Boundary.YMin);
        }

        /// <summary>
        /// 地图的显示区域发生改变，清空选择框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArcMapViewpointChanged(object sender, EventArgs e)
        {
            // cvs.StopDrawing(false);
        }

        private async Task CalculateTileNumberAsync(bool save = true)
        {
            if (CurrentDownload == null)
            {
                CurrentDownload = new DownloadInfo();
            }
            DownloadingProgressPercent = 0;
            GeoRect<double> value = downloadBoundary.GetDoubleValue();
            if (value != null)
            {
                CurrentDownload.SetRange(value);
                DownloadingProgressStatus = "共" + CurrentDownload.TileCount;

                var (tile1X, tile1Y) = TileLocation.PointToTile(value.YMax_Top, value.XMin_Left, cbbLevel.SelectedIndex);
                var (tile2X, tile2Y) = TileLocation.PointToTile(value.YMin_Bottom, value.XMax_Right, cbbLevel.SelectedIndex);
                stichBoundary.SetIntValue(tile1X, tile1Y, tile2X, tile2Y);
                if (save)
                {
                    Config.Instance.Tile_LastDownload = CurrentDownload;
                    Config.Save();
                }
            }
            else
            {
                await CommonDialog.ShowErrorDialogAsync("坐标范围不正确");
            }
        }

        private async void CalculateTileNumberButtonClick(object sender, RoutedEventArgs e)
        {
            await CalculateTileNumberAsync();
            lastTile = null;
            CurrentDownloadStatus = DownloadStatus.Stop;
        }

        private async void DeleteEmptyFilesButtonClick(object sender, RoutedEventArgs e)
        {
            await DoAsync(async () =>
            {
                try
                {
                    string[] files = null;
                    await Task.Run(() => files = Directory.EnumerateFiles(Config.Instance.Tile_DownloadFolder, "*", SearchOption.AllDirectories)
                    .Where(p => new FileInfo(p).Length == 0).ToArray());
                    if (files.Length == 0)
                    {
                        await CommonDialog.ShowErrorDialogAsync("没有空文件");
                    }
                    else
                    {
                        if (await CommonDialog.ShowYesNoDialogAsync($"共找到{files.Length}个空文件，是否删除？") == true)
                        {
                            foreach (var file in files)
                            {
                                File.Delete(file);
                            }
                            await CommonDialog.ShowOkDialogAsync("删除空文件", "删除成功");
                        }
                    };
                }
                catch (Exception ex)
                {
                    App.Log.Error("删除空文件失败", ex);
                    await CommonDialog.ShowErrorDialogAsync(ex, "删除失败");
                }
            }, "正在删除");
        }

        private void DeleteTileSourceButtonClick(object sender, RoutedEventArgs e)
        {
            Config.Tile_Urls.Sources.Remove(Config.Tile_Urls.SelectedUrl);
        }

        private async void DownloadButtonClick(object sender, RoutedEventArgs e)
        {
            if (Config.Instance.Tile_Urls.SelectedUrl == null)
            {
                await CommonDialog.ShowErrorDialogAsync("还未选择瓦片地址");
                return;
            }
            if (CurrentDownload == null)
            {
                await CommonDialog.ShowErrorDialogAsync("还没有进行设置");
                return;
            }

            if (CurrentDownloadStatus == DownloadStatus.Stop || CurrentDownloadStatus == DownloadStatus.Paused)
            {
                await StartOrContinueDowloadingAsync();
            }
            else
            {
                StopDownloading();
            }
        }

        private void ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            tbkStichStatus.Text = "图片显示失败，但文件可能已经保存";
        }

        private async void LevelSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CurrentDownload != null)
            {
                await CalculateTileNumberAsync();
            }
        }

        private void NewTileSourceButtonClick(object sender, RoutedEventArgs e)
        {
            TileSourceInfo tile = new TileSourceInfo();
            if (Config.Tile_Urls.SelectedIndex == -1)
            {
                Config.Tile_Urls.Sources.Add(tile);
                Config.Tile_Urls.SelectedIndex = Config.Tile_Urls.Sources.Count - 1;
            }
            else
            {
                Config.Tile_Urls.Sources.Insert(Config.Tile_Urls.SelectedIndex + 1, tile);
                Config.Tile_Urls.SelectedIndex = Config.Tile_Urls.SelectedIndex + 1;
            }
            dgrdUrls.ScrollIntoView(tile);
        }

        private async void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(Config.Tile_DownloadFolder))
                {
                    Directory.CreateDirectory(Config.Tile_DownloadFolder);
                }
                await IOUtility.TryOpenFolderAsync(Config.Tile_DownloadFolder);
            }
            catch (Exception ex)
            {
                App.Log.Error("无法打开目录", ex);
                await CommonDialog.ShowErrorDialogAsync(ex, "无法打开目录");
            }
        }

        private async void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (savedImgPath != null)
            {
                if (File.Exists(savedImgPath))
                {
                    var file = new FileFilterCollection()
                        .Add("TIFF图片", "tif")
                        .CreateSaveFileDialog()
                        .SetDefault("地图.tif")
                        .SetParent(this)
                        .GetFilePath();
                    if (file != null)
                    {
                        tbkStichStatus.Text = "正在保存地图";
                        await Task.Run(() =>
                        {
                            try
                            {
                                if (File.Exists(file))
                                {
                                    File.Delete(file);
                                }
                                File.Copy(savedImgPath, file);
                                if (currentProject != null)
                                {
                                    string worldFile = file + "w";
                                    string projFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".prj");

                                    if (File.Exists(worldFile))
                                    {
                                        File.Delete(worldFile);
                                    }
                                    if (File.Exists(projFile))
                                    {
                                        File.Delete(projFile);
                                    }
                                    File.WriteAllText(worldFile, currentProject.ToString());
                                    File.WriteAllText(projFile, SpatialReferences.WebMercator.WkText);
                                }
                            }
                            catch (Exception ex)
                            {
                                App.Log.Error("保存瓦片拼接失败", ex);
                                Dispatcher.Invoke(async () =>
                                {
                                    await CommonDialog.ShowErrorDialogAsync(ex, "保存失败");
                                });
                            }
                        });

                        tbkStichStatus.Text = "";
                    }
                }
                else
                {
                    await CommonDialog.ShowErrorDialogAsync("没有已生成的地图！");
                }
            }
        }

        private async void SelectAreaButtonClick(object sender, RoutedEventArgs e)
        {
            await arcMap.SelectAsync();
        }

        private async void ServerButtonClick(object sender, RoutedEventArgs e)
        {
            if (!ServerOn)
            {
                ServerOn = true;
                try
                {
                    NetUtility.StartServer(Config.Tile_ServerPort, Config.Tile_ServerFilePathFormat.Replace("{Download}", FolderPaths.TileDownloadPath), Config.Tile_FormatExtension);
                }
                catch (SocketException sex)
                {
                    switch (sex.ErrorCode)
                    {
                        case 10048:
                            await CommonDialog.ShowErrorDialogAsync("端口不可用，清更换端口");
                            break;

                        default:
                            await CommonDialog.ShowErrorDialogAsync(sex, "开启服务失败");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    App.Log.Error("开启服务失败", ex);
                    await CommonDialog.ShowErrorDialogAsync(ex, "开启服务失败");
                }
            }
            else
            {
                ServerOn = false;
                NetUtility.StopServer();
            }
            // 关闭服务器
            // tcpListener.Stop();
        }

        private async Task StartOrContinueDowloadingAsync()
        {
            CurrentDownloadStatus = DownloadStatus.Downloading;
            //pgb.Maximum = CurrentDownload.TileCount;
            DownloadErrors.Clear();
            int ok = 0;
            int failed = 0;
            int skip = lastTile == null ? 0 : lastIndex;
            string baseUrl = Config.Tile_Urls.SelectedUrl.Url;
            arcMap.SketchEditor.IsEnabled = false;
            await Task.Run(async () =>
            {
                IEnumerator<TileInfo> enumerator = CurrentDownload.GetEnumerator(lastTile);
                while (enumerator.MoveNext())
                {
                    TileInfo tile = enumerator.Current;

                    string path = Path.Combine(Config.Tile_DownloadFolder, tile.Level.ToString(), $"{tile.X}-{tile.Y}.{ Config.Instance.Tile_FormatExtension}");

                    try
                    {
                        LastDownloadingTile = $"Z={tile.Level}  X={tile.X}  Y={tile.Y}";
                        if (!File.Exists(path) || Config.Tile_CoverFile)
                        {
                            string url = baseUrl.Replace("{x}", tile.X.ToString()).Replace("{y}", tile.Y.ToString()).Replace("{z}", tile.Level.ToString());
                            arcMap.ShowPosition(this, tile);
                            await NetUtility.HttpDownloadAsync(url, path, TimeSpan.FromMilliseconds(Config.HttpTimeOut), Config.Tile_DownloadUserAgent);
                            //Dispatcher.Invoke(() => tile.Status = "完成");
                            LastDownloadingStatus = "下载成功";

                            ok++;
                        }
                        else
                        {
                            LastDownloadingStatus = "跳过：文件已存在";
                            skip++;
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error("下载瓦片失败", ex);
                        LastDownloadingStatus = "失败：" + ex.Message;
                        DownloadErrors.Add(new { Tile = LastDownloadingTile, Error = ex.Message, StackTrace = ex.StackTrace.Replace(Environment.NewLine, "    ") });
                        failed++;
                    }
                    //DownloadingProgressValue = ok + failed + skip;
                    DownloadingProgressPercent = 1.0 * (ok + failed + skip) / CurrentDownload.TileCount;
                    DownloadingProgressStatus = $"成功{ ok } 失败{ failed} 跳过{ skip } 共{ CurrentDownload.TileCount}";
                    //taskBar.ProgressValue = (ok + failed + skip) * 1d / CurrentDownload.TileCount;
                    //处理点击停止按钮后的逻辑
                    if (CurrentDownloadStatus == DownloadStatus.Pausing)
                    {
                        lastTile = tile;
                        lastIndex = ok + skip + failed;
                        return;
                    }
                }
                lastTile = null;
            });
            LastDownloadingStatus = "下载结束";
            CurrentDownloadStatus = lastTile == null ? DownloadStatus.Stop : DownloadStatus.Paused;
            arcMap.ShowPosition(this, null);
            arcMap.SketchEditor.IsEnabled = true;

            if (await CommonDialog.ShowYesNoDialogAsync("下载完成，是否删除临时文件夹？") == true)
            {
                DoAsync(() => Task.Run(() =>
                     {
                         try
                         {
                             foreach (var directory in Directory.EnumerateDirectories(Config.Tile_DownloadFolder, "temp", SearchOption.AllDirectories).ToArray())
                             {
                                 FzLib.IO.WindowsFileSystem.DeleteFileOrFolder(directory, true, false);
                             }
                         }
                         catch (Exception ex)
                         {
                             App.Log.Error("无法删除临时文件夹", ex);
                             Dispatcher.Invoke(async () => await CommonDialog.ShowErrorDialogAsync(ex, "无法删除临时文件夹"));
                         }
                     })
                , "正在删除临时文件夹");
            }

            if (closing)
            {
                Close();
            }
        }

        private async void StichButtonClick(object sender, RoutedEventArgs e)
        {
            if (btnStich.Content as string == "开始拼接")
            {
                int level = cbbLevel.SelectedIndex;
                var tryBound = stichBoundary.GetIntValue();
                if (tryBound == null)
                {
                    await CommonDialog.ShowErrorDialogAsync("瓦片边界输入错误");
                    return;
                }
                var boundary = stichBoundary.GetIntValue();
                int right = boundary.XMax_Right;
                int left = boundary.XMin_Left;
                int bottom = boundary.YMin_Bottom;
                int top = boundary.YMax_Top;
                int width = Config.Instance.Tile_TileSize.width * (right - left + 1);
                int height = Config.Instance.Tile_TileSize.height * (bottom - top + 1);

                savedImgPath = "TempSaveImg\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + Config.Instance.Tile_FormatExtension;
                savedImgPath = new FileInfo(savedImgPath).FullName;
                tab.SelectedIndex = 1;
                btnStich.Content = "终止拼接";

                ControlsEnable = false;
                await Task.Run(() =>
                 {
                     try
                     {
                         Bitmap bitmap = null;
                         try
                         {
                             bitmap = new Bitmap(width, height);
                         }
                         catch
                         {
                             Dispatcher.Invoke(async () =>
                            {
                                await CommonDialog.ShowErrorDialogAsync("内存不足");
                            });
                             return;
                         }
                         int count = (right - left + 1) * (bottom - top + 1);
                         int index = 0;
                         Graphics graphics = Graphics.FromImage(bitmap);
                         for (int i = left; i <= right; i++)
                         {
                             for (int j = top; j <= bottom; j++)
                             {
                                 if (stopStich)
                                 {
                                     bitmap.Dispose();
                                     return;
                                 }
                                 string fileName = Config.Tile_DownloadFolder + "\\" + level + "\\" + i + "-" + j + "." + Config.Instance.Tile_FormatExtension;
                                 if (File.Exists(fileName))
                                 {
                                     graphics.DrawImage(Bitmap.FromFile(fileName), Config.Instance.Tile_TileSize.width * (i - left), Config.Instance.Tile_TileSize.height * (j - top), 256, 256);
                                     //graphics.DrawString(level + "\\" + i + "-" + j, new Font(new FontFamily("微软雅黑"), 20), Brushes.Black, length * (i - left), length * (j - top));
                                 }
                                 Dispatcher.Invoke(() =>
                                 {
                                     tbkStichStatus.Text = (++index) + "/" + count;
                                 });
                             }
                         }
                         Dispatcher.Invoke(() =>
                         {
                             tbkStichStatus.Text = "正在保存图片";
                         });
                         if (!new FileInfo(savedImgPath).Directory.Exists)
                         {
                             new FileInfo(savedImgPath).Directory.Create();
                         }
                         bitmap.Save(savedImgPath, ImageFormat.Tiff);
                         bitmap.Dispose();
                     }
                     catch (Exception ex)
                     {
                         App.Log.Error("拼接图片失败", ex);
                         Dispatcher.Invoke(async () =>
                         {
                             await CommonDialog.ShowErrorDialogAsync(ex, "拼接图片失败");
                         });
                     }
                 });
                if (stopStich)
                {
                    stopStich = false;
                }
                else if (File.Exists(savedImgPath))
                {
                    staticMap.Source = new BitmapImage(new Uri(savedImgPath));
                    currentProject = new ProjectInfo(level, left, top);
                }
                tbkStichStatus.Text = "";

                btnStich.Content = "开始拼接";
                ControlsEnable = true;
            }
            else
            {
                stopStich = true;
            }
        }

        private void StopDownloading()
        {
            CurrentDownloadStatus = DownloadStatus.Pausing;
        }

        private async void tab_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tab.SelectedIndex == 2)
            {
                if (!ServerOn)
                {
                    ServerButtonClick(null, null);
                }
                await arcLocalMap.LoadAsync();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Config.Tile_LastDownload != null)
            {
                CurrentDownload = Config.Tile_LastDownload;
                if (CurrentDownload != null)
                {
                    downloadBoundary.SetDoubleValue(CurrentDownload.MapRange.XMin_Left, CurrentDownload.MapRange.YMax_Top,
                        CurrentDownload.MapRange.XMax_Right, CurrentDownload.MapRange.YMin_Bottom);
                    await CalculateTileNumberAsync(false);
                    arcMap.SetBoundary(CurrentDownload.MapRange);
                }
            }
            else
            {
                CurrentDownload = new DownloadInfo();
            }
        }

        private async void WindowClosing(object sender, CancelEventArgs e)
        {
            if (CurrentDownloadStatus == DownloadStatus.Downloading)
            {
                e.Cancel = true;
                if (await CommonDialog.ShowYesNoDialogAsync("正在下载瓦片，是否停止下载后关闭窗口？"))
                {
                    closing = true;
                    StopDownloading();
                }
            }
            Config.Instance.Tile_LastDownload = CurrentDownload;

            Config.Save();
        }
    }
}