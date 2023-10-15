using MapBoard.IO;
using MapBoard.Mapping;
using MapBoard.Models;
using MapBoard.Services;
using MapBoard.Util;
using MapBoard.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace MapBoard.Views;

public partial class ImportView : ContentView, ISidePanel
{
    public ImportView()
    {
        BindingContext = new ImportViewVideModel();
        InitializeComponent();

    }

    public void OnPanelClosed()
    {
    }

    public async void OnPanelOpening()
    {
        await (BindingContext as ImportViewVideModel).LoadFilesAsync();
    }

    private void ContentView_Loaded(object sender, EventArgs e)
    {
    }

    private async Task ImportAsync(string file)
    {
        var handle = PlatformDialog.ShowLoading("正在导入地图");
        try
        {
            await Package.ImportMapAsync(file, MainMapView.Current.Layers, true);
            MainPage.Current.ClosePanel<ImportView>();
        }
        catch (Exception ex)
        {
            await MainPage.Current.DisplayAlert("加载失败", ex.Message, "确定");
        }
        finally
        {
            PlatformDialog.DismissLoading(handle);
        }
    }

    private async void ImportButton_Clicked(object sender, EventArgs e)
    {
        PickOptions options = new PickOptions()
        {
            PickerTitle = "选取mbmpkg地图包文件",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>()
            {
                [DevicePlatform.Android] = new[] { "application/octet-stream" },
                [DevicePlatform.WinUI] = new[] { "*.mbmpkg" }
            })
        };
        var file = await FilePicker.Default.PickAsync(options);
        if (file != null)
        {
            await ImportAsync(file.FullPath);
        }
    }

    private async void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        var file = e.Item as SimpleFile;
        await ImportAsync(file.FullName);
    }
}