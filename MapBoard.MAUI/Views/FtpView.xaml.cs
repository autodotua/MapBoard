using CommunityToolkit.Maui.Alerts;
using MapBoard.IO;
using MapBoard.Services;
using MapBoard.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace MapBoard.Views;

public partial class FtpView : ContentView, ISidePanel
{
    FtpService ftpService;

    public FtpView()
    {
        BindingContext = new FtpViewViewModel();
        InitializeComponent();
#if WINDOWS
        btnFtp.IsVisible = false;
            btnOpenDir.IsVisible = true;
            grdFtpInfo.IsVisible = false;
#endif

#if !ANDROID
    lblSaveGpx.IsVisible=false;
#endif
    }

    public SwipeDirection Direction => SwipeDirection.Left;

    public int Length => 300;

    public bool Standalone => false;

    public void OnPanelOpened()
    {
        var ip = FtpService.GetIpAddress();
        if (ip == null || !ip.Any())
        {
            ip = null;
        }
        (BindingContext as FtpViewViewModel).IP = ip == null ? "未知" : string.Join(Environment.NewLine, ip);
    }

    private string GetSelectedDir()
    {
        string dir = null;
        if (rbtnDataDir.IsChecked)
        {
            dir = FolderPaths.DataPath;
        }
        else if (rbtnLogDir.IsChecked)
        {
            dir = FolderPaths.LogsPath;
        }
        else if (rbtnTrackDir.IsChecked)
        {
            dir = FolderPaths.TrackPath;
        }
        else if (rbtnRootDir.IsChecked)
        {
            dir = FileSystem.AppDataDirectory;
        }
        else if (rbtnCacheDir.IsChecked)
        {
            dir = FileSystem.CacheDirectory;
        }
        else if (rbtnPackageDir.IsChecked)
        {
            dir = FolderPaths.PackagePath;
        }

        return dir;
    }

    private void OpenDirButton_Clicked(object sender, EventArgs e)
    {
#if WINDOWS
        string dir = GetSelectedDir();
        Process.Start("explorer.exe", dir);
#else
        throw new NotSupportedException();
#endif
    }

    private async void StartStopFtpButton_Clicked(object sender, EventArgs e)
    {
        if ((BindingContext as FtpViewViewModel).IsOn)
        {
            stkFtpDirs.IsEnabled = true;
            (sender as Button).Text = "打开FTP";
            await ftpService.StopServerAsync();
            ftpService = null;
        }
        else
        {
            stkFtpDirs.IsEnabled = false;
            string dir = GetSelectedDir();
            ftpService = new FtpService(dir);
            await ftpService.StartServerAsync();
            (sender as Button).Text = "关闭FTP";
        }
        (BindingContext as FtpViewViewModel).IsOn = !(BindingContext as FtpViewViewModel).IsOn;
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var handle = ProgressPopup.Show("正在保存");
#if ANDROID
        try
        {
            var root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            root = new Java.IO.File(root, AppInfo.Name);
            root.Mkdir();

            await Task.Run(() =>
            {
                var gpxFiles = Directory.EnumerateFiles(FolderPaths.TrackPath, "*.gpx").ToList();
                foreach (var name in gpxFiles)
                {
                    var dist = Path.Combine(root.Path, Path.GetFileName(name));
                    File.Copy(name, dist, true);
                    File.SetLastWriteTime(dist, File.GetLastWriteTime(name));
                }
            });
            await Toast.Make("已保存到" + root.Path,CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
        }
        catch (Exception ex)
        {
            await MainPage.Current.DisplayAlert("保存失败", ex.Message, "确定");
        }

#else
        throw new NotSupportedException();
#endif
        handle.Close();
    }
}