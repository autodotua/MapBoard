using MapBoard.IO;
using MapBoard.Services;
using MapBoard.ViewModels;
using System.Globalization;
using System.Linq;

namespace MapBoard.Pages;

public partial class FtpPage : ContentView
{
    FtpService ftpService;
    public FtpPage()
    {
        var ip = FtpService.GetIpAddress();
        if (ip == null || !ip.Any())
        {
            ip = null;
        }
        BindingContext = new FtpPageViewModel()
        {
            IP = ip == null ? "未知" : string.Join(Environment.NewLine, ip),
            IsOn = false,
        };
        InitializeComponent();

    }

    private void StartStopFtpButton_Clicked(object sender, EventArgs e)
    {
        if ((BindingContext as FtpPageViewModel).IsOn)
        {
            stkFtpDirs.IsEnabled= true;
            ftpService.StopServerAsync();
            ftpService = null;
            (sender as Button).Text = "打开FTP";
        }
        else
        {
            stkFtpDirs.IsEnabled = false;
            string dir = null;
            if(rbtnDataDir.IsChecked)
            {
                dir = FolderPaths.DataPath;
            }
            else if(rbtnLogDir.IsChecked)
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
            ftpService = new FtpService(dir);
            ftpService.StartServerAsync();
            (sender as Button).Text = "关闭FTP";
        }
        (BindingContext as FtpPageViewModel).IsOn = !(BindingContext as FtpPageViewModel).IsOn;
    }
}