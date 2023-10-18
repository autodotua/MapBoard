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
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            btnFtp.IsVisible = false;
            btnOpenDir.IsVisible = true;
            grdFtpInfo.IsVisible = false;
        }
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
        if(DeviceInfo.Platform!=DevicePlatform.WinUI)
        {
            throw new NotSupportedException("仅支持Windows");
        }
        string dir = GetSelectedDir();
        Process.Start("explorer.exe", dir);
    }

    public void OnPanelOpening()
    {
        var ip = FtpService.GetIpAddress();
        if (ip == null || !ip.Any())
        {
            ip = null;
        }
        (BindingContext as FtpViewViewModel).IP = ip == null ? "未知" : string.Join(Environment.NewLine, ip);
    }

    public void OnPanelClosed()
    {
    }
}