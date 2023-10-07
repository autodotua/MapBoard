using MapBoard.Services;
using MapBoard.ViewModels;
using System.Globalization;
using System.Linq;

namespace MapBoard.Pages;

public partial class FtpPage : ContentPage
{
    FtpService ftpService = new FtpService();
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
            ftpService.StopServerAsync();
            (sender as Button).Text = "打开FTP";
        }
        else
        {
            ftpService.StartServerAsync();
            (sender as Button).Text = "关闭FTP";
        }
        (BindingContext as FtpPageViewModel).IsOn = !(BindingContext as FtpPageViewModel).IsOn;
    }
}