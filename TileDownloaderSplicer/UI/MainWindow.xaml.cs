using Esri.ArcGISRuntime.Geometry;
using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using FzLib.Geography.IO.Tile;
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
using GeoPoint = NetTopologySuite.Geometries.Point;
using ModernWpf.FzExtension.CommonDialog;
using MapBoard.TileDownloaderSplicer.Model;

namespace MapBoard.TileDownloaderSplicer.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : ExtendedWindow
    {
        public Config Config => Config.Instance;

        /// <summary>
        /// 左上角的点
        /// </summary>
        //  MapPoint leftUpPoint;
        //  /// <summary>
        /// 右下角的点
        /// </summary>
        //  MapPoint rightDownPoint;
        /// <summary>
        /// 控制在执行耗时工作时控件的可用性
        /// </summary>
        private bool controlsEnable = true;

        /// <summary>
        /// 是否有终止拼接的命令
        /// </summary>
        private bool stopStich = false;

        /// <summary>
        /// 暂停时最后一块瓦片
        /// </summary>
        private TileInfo lastTile = null;

        /// <summary>
        /// 暂停时瓦片的序号
        /// </summary>
        private int lastIndex = 0;

        ///// <summary>
        ///// 是否正在下载
        ///// </summary>
        //private bool downloading = false;

        /// <summary>
        /// 保存的拼接完成后临时图片的位置
        /// </summary>
        private string savedImgPath = null;

        private bool closing = false;

        public IReadOnlyList<string> Formats { get; } = new List<string> { "jpg", "png", "bmp", "tiff" }.AsReadOnly();

        /// <summary>
        /// 当前投影信息
        /// </summary>
        private ProjectInfo currentProject;

        public ObservableCollection<dynamic> DownloadErrors { get; } = new ObservableCollection<dynamic>();
        private string lastdownloadingTile = "还未下载";
        public string LastDownloadingTile { get => lastdownloadingTile; set => SetValueAndNotify(ref lastdownloadingTile, value, nameof(LastDownloadingTile)); }
        private string lastdownloadingStatus = "准备就绪";
        public string LastDownloadingStatus { get => lastdownloadingStatus; set => SetValueAndNotify(ref lastdownloadingStatus, value, nameof(LastDownloadingStatus)); }
        private string downloadingProgressStatus = "准备就绪";
        public string DownloadingProgressStatus { get => downloadingProgressStatus; set => SetValueAndNotify(ref downloadingProgressStatus, value, nameof(DownloadingProgressStatus)); }

        //private int downloadingProgressValue = 0;
        //public int DownloadingProgressValue
        //{
        //    get => downloadingProgressValue;
        //    set
        //    {
        //        SetValueAndNotify(ref downloadingProgressValue, value, nameof(DownloadingProgressValue));
        //        DownloadingProgressPercent = (double)value / CurrentDownload.TileCount;
        //    }
        //}
        private double downloadingProgressPercent = 0;

        public double DownloadingProgressPercent { get => downloadingProgressPercent; set => SetValueAndNotify(ref downloadingProgressPercent, value, nameof(DownloadingProgressPercent)); }
        private DownloadStatus currentDownloadStatus = DownloadStatus.Stop;

        public DownloadStatus CurrentDownloadStatus
        {
            get => currentDownloadStatus;
            set
            {
                SetValueAndNotify(ref currentDownloadStatus, value, nameof(CurrentDownloadStatus));
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

        private bool serverOn = false;

        public bool ServerOn
        {
            get => serverOn;
            set => SetValueAndNotify(ref serverOn, value, nameof(ServerOn));
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
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

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
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

        private async void SelectAreaButtonClick(object sender, RoutedEventArgs e)
        {
            await arcMap.SelectAsync();
        }

        private DownloadInfo currentDownload;

        public DownloadInfo CurrentDownload
        {
            get => currentDownload;
            set => SetValueAndNotify(ref currentDownload, value, nameof(CurrentDownload));
        }

        public bool ControlsEnable
        {
            get => controlsEnable;
            set => SetValueAndNotify(ref controlsEnable, value, nameof(ControlsEnable));
        }

        private bool waiting = false;

        private async void CalculateTileNumberButtonClick(object sender, RoutedEventArgs e)
        {
            await CalculateTileNumberAsync();
            lastTile = null;
            CurrentDownloadStatus = DownloadStatus.Stop;
        }

        private async Task CalculateTileNumberAsync(bool save = true)
        {
            if (CurrentDownload == null)
            {
                CurrentDownload = new DownloadInfo();
            }
            DownloadingProgressPercent = 0;
            GeoRange<double> value = downloadBoundary.GetDoubleValue();
            if (value != null)
            {
                CurrentDownload.SetRange(value);
                DownloadingProgressStatus = "共" + CurrentDownload.TileCount;

                var (tile1X, tile1Y) = TileLocation.GeoPointToTile(value.YMax_Top, value.XMin_Left, cbbLevel.SelectedIndex);
                var (tile2X, tile2Y) = TileLocation.GeoPointToTile(value.YMin_Bottom, value.XMax_Right, cbbLevel.SelectedIndex);
                stichBoundary.SetIntValue(tile1X, tile1Y, tile2X, tile2Y);
                if (save)
                {
                    Config.Instance.LastDownload = CurrentDownload;
                    Config.Save();
                }
            }
            else
            {
                await CommonDialog.ShowErrorDialogAsync("坐标范围不正确");
            }
        }

        private async void DownloadButtonClick(object sender, RoutedEventArgs e)
        {
            if (Config.Instance.UrlCollection.SelectedUrl == null)
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

        private void StopDownloading()
        {
            CurrentDownloadStatus = DownloadStatus.Pausing;
        }

        private async Task StartOrContinueDowloadingAsync()
        {
            CurrentDownloadStatus = DownloadStatus.Downloading;
            //pgb.Maximum = CurrentDownload.TileCount;
            DownloadErrors.Clear();
            int ok = 0;
            int failed = 0;
            int skip = lastTile == null ? 0 : lastIndex;
            string baseUrl = Config.UrlCollection.SelectedUrl.Url;
            arcMap.SketchEditor.IsEnabled = false;
            await Task.Run(() =>
            {
                IEnumerator<TileInfo> enumerator = CurrentDownload.GetEnumerator(lastTile);
                while (enumerator.MoveNext())
                {
                    TileInfo tile = enumerator.Current;

                    string path = Path.Combine(Config.DownloadFolder, tile.Level.ToString(), $"{tile.X}-{tile.Y}.{ Config.Instance.FormatExtension}");

                    try
                    {
                        LastDownloadingTile = $"Z={tile.Level}  X={tile.X}  Y={tile.Y}";
                        if (!File.Exists(path) || Config.CoverFile)
                        {
                            string url = baseUrl.Replace("{x}", tile.X.ToString()).Replace("{y}", tile.Y.ToString()).Replace("{z}", tile.Level.ToString());
                            arcMap.ShowPosition(this, tile);
                            NetHelper.HttpDownload(url, path);
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
                loading.Show();
                await Task.Run(() =>
                {
                    try
                    {
                        foreach (var directory in Directory.EnumerateDirectories(Config.DownloadFolder, "temp", SearchOption.AllDirectories).ToArray())
                        {
                            FzLib.IO.WindowsFileSystem.DeleteFileOrFolder(directory, true, false);
                            //Directory.Delete(directory, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(async () => await CommonDialog.ShowErrorDialogAsync(ex, "无法删除临时文件夹"));
                    }
                });
                loading.Hide();
            }

            if (closing)
            {
                Close();
            }
        }

        private async void ServerButtonClick(object sender, RoutedEventArgs e)
        {
            if (!ServerOn)
            {
                ServerOn = true;
                try
                {
                    NetHelper.StartServer();
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
                    await CommonDialog.ShowErrorDialogAsync(ex, "开启服务失败");
                }
            }
            else
            {
                ServerOn = false;
                NetHelper.StopServer();
            }
            // 关闭服务器
            // tcpListener.Stop();
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
                int width = Config.Instance.TileSize.width * (right - left + 1);
                int height = Config.Instance.TileSize.height * (bottom - top + 1);

                savedImgPath = "TempSaveImg\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + Config.Instance.FormatExtension;
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
                                await CommonDialog.ShowErrorDialogAsync("图片尺寸对于内存来说太大");
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
                                 string fileName = Config.DownloadFolder + "\\" + level + "\\" + i + "-" + j + "." + Config.Instance.FormatExtension;
                                 if (File.Exists(fileName))
                                 {
                                     graphics.DrawImage(Bitmap.FromFile(fileName), Config.Instance.TileSize.width * (i - left), Config.Instance.TileSize.height * (j - top), 256, 256);
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
                         bitmap.Save(savedImgPath, Config.ImageFormat);
                         bitmap.Dispose();
                     }
                     catch (Exception ex)
                     {
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

        private async void LevelSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CurrentDownload != null)
            {
                await CalculateTileNumberAsync();
            }
        }

        private void ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            tbkStichStatus.Text = "图片显示失败，但文件可能已经保存";
        }

        private async void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (savedImgPath != null)
            {
                if (File.Exists(savedImgPath))
                {
                    var file = FileSystemDialog.GetSaveFile(new FileFilterCollection().Add(Config.FormatExtension + "图片", Config.FormatExtension), true, "地图." + Config.FormatExtension);
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
                                    File.WriteAllText(projFile, Common.Resource.Proj3857);
                                }
                            }
                            catch (Exception ex)
                            {
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
            Config.Instance.LastDownload = CurrentDownload;

            Config.Save();
        }

        private async void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(Config.DownloadFolder))
                {
                    Directory.CreateDirectory(Config.DownloadFolder);
                }
                Process.Start("explorer.exe", Config.DownloadFolder);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "无法打开目录");
            }
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

        private void arcMap_SelectBoundaryComplete(object sender, EventArgs e)
        {
            downloadBoundary.SetDoubleValue(arcMap.Boundary.XMin, arcMap.Boundary.YMax, arcMap.Boundary.XMax, arcMap.Boundary.YMin);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Config.LastDownload != null)
            {
                CurrentDownload = Config.LastDownload;
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

        private void arcMap_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (waiting)
            {
                return;
            }
            ArcMapView map = sender as ArcMapView;
            if (map.Map == null || map.Map.LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                return;
            }
            MapPoint point = map.ScreenToLocation(e.GetPosition(sender as IInputElement));

            point = GeometryEngine.Project(point, SpatialReferences.Wgs84) as MapPoint;
            int z = (int)(Math.Log(1e9 / map.MapScale, 2));
            (int x, int y) = TileLocation.GeoPointToTile(new GeoPoint(point.X, point.Y), z);
            string l = Environment.NewLine;
            tbkTileIndex.Text = $"Z={z}{l}X={x}{l}Y={y}";
            waiting = true;
            Task.Delay(250).ContinueWith(p => waiting = false);
        }

        private void NewTileSourceButtonClick(object sender, RoutedEventArgs e)
        {
            TileSourceInfo tile = new TileSourceInfo();
            if (dgrdUrls.SelectedIndex == -1)
            {
                Config.UrlCollection.Sources.Add(tile);
            }
            else
            {
                Config.UrlCollection.Sources.Insert(dgrdUrls.SelectedIndex + 1, tile);
            }
            dgrdUrls.SelectedItem = tile;
            dgrdUrls.ScrollIntoView(tile);
        }

        private void DeleteTileSourceButtonClick(object sender, RoutedEventArgs e)
        {
            Config.UrlCollection.Sources.Remove(dgrdUrls.SelectedItem as TileSourceInfo);
        }

        public void SetLoading(bool isLoading)
        {
            loading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void DeleteEmptyFilesButtonClick(object sender, RoutedEventArgs e)
        {
            loading.Show();
            try
            {
                string[] files = null;
                await Task.Run(() => files = Directory.EnumerateFiles(Config.Instance.DownloadFolder, "*", SearchOption.AllDirectories)
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
                await CommonDialog.ShowErrorDialogAsync(ex, "删除失败");
            }
            finally
            {
                loading.Hide();
            }
        }
    }

    public enum DownloadStatus
    {
        Downloading,
        Paused,
        Stop,
        Pausing
    }
}