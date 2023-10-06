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
        BindingContext = new FtpPageViewModels()
        {
            IP = string.Join(Environment.NewLine, FtpService.GetIpAddress()),
            IsOn = false,
        };
        InitializeComponent();

    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }

    private void StartStopFtpButton_Clicked(object sender, EventArgs e)
    {
        if ((BindingContext as FtpPageViewModels).IsOn)
        {
            ftpService.StopServerAsync();
            (sender as Button).Text = "´ò¿ªFTP";
        }
        else
        {
            ftpService.StartServerAsync();
            (sender as Button).Text = "¹Ø±ÕFTP";
        }
        (BindingContext as FtpPageViewModels).IsOn = !(BindingContext as FtpPageViewModels).IsOn;
    }
}