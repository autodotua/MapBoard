#if ANDROID
using Android.App;
using Android.Content;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.TextField;
#endif
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.ViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Application = Microsoft.Maui.Controls.Application;

namespace MapBoard.Views;

public partial class BaseLayerView : ContentView
{
    public BaseLayerView()
    {
        InitializeComponent();
        BindingContext = new BaseLayerViewViewModel();

    }
    private async Task AddLayerAsync(string name, string url)
    {
        IsEnabled = false;
        try
        {
            var baseLayer = new BaseLayerInfo()
            {
                Name = name,
                Path = url,
                Type = BaseLayerType.WebTiledLayer
            };
            (BindingContext as BaseLayerViewViewModel).BaseLayers.Insert(0, baseLayer);
            (BindingContext as BaseLayerViewViewModel).Save();
            await MapViewHelper.AddLayerAsync(MainMapView.Current.Map.Basemap, baseLayer, true);
        }
        catch (Exception ex)
        {
            await MainPage.Current.DisplayAlert("添加失败", ex.Message, "确定");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void AddNewBaseLayerButton_Clicked(object sender, EventArgs e)
#else
    private async void AddNewBaseLayerButton_Clicked(object sender, EventArgs e)
#endif
    {
#if ANDROID
        ShowAndroidDialog(false);
#else
        throw new NotImplementedException();
        string url = await MainPage.Current.DisplayPromptAsync("新增底图", "输入XYZ瓦片地图链接", "确定", "取消");
        if (!string.IsNullOrEmpty(url))
        {
            (BindingContext as BaseLayerViewViewModel).BaseLayers.Add(new Model.BaseLayerInfo()
            {
                Path = url,
                Type = Model.BaseLayerType.WebTiledLayer
            });
        }
#endif
    }

    private void ContentView_Loaded(object sender, EventArgs e)
    {

    }
#if ANDROID
#if ANDROID
    private void DeleteSwipeItem_Clicked(object sender, EventArgs e)
    {
        var view = sender as SwipeItem;
        if (view.BindingContext is BaseLayerInfo baseLayer)
        {
            (BindingContext as BaseLayerViewViewModel).BaseLayers.Remove(baseLayer);
            (BindingContext as BaseLayerViewViewModel).Save();
            var esriBaseLayer = MainMapView.Current.Map.Basemap.BaseLayers.First(p => p.Name == baseLayer.Name);
            MainMapView.Current.Map.Basemap.BaseLayers.Remove(esriBaseLayer);
        }
    }

    private async Task ModifyLayerAsync(BaseLayerInfo baseLayer, string name, string url)
    {
        IsEnabled = false;
        try
        {
            (BindingContext as BaseLayerViewViewModel).Save();
            var esriBaseLayers = MainMapView.Current.Map.Basemap.BaseLayers;
            var esriBaseLayer = esriBaseLayers.First(p => p.Name == baseLayer.Name);
            int index = esriBaseLayers.IndexOf(esriBaseLayer);
            esriBaseLayers.RemoveAt(index);
            baseLayer.Name = name;
            baseLayer.Path = url;
            await MapViewHelper.AddLayerAsync(MainMapView.Current.Map.Basemap, baseLayer, true, index);
        }
        catch (Exception ex)
        {
            await MainPage.Current.DisplayAlert("添加失败", ex.Message, "确定");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void ModifySwipeItem_Clicked(object sender, EventArgs e)
    {
        var view = sender as SwipeItem;
        if (view.BindingContext is BaseLayerInfo baseLayer)
        {
#if ANDROID
            ShowAndroidDialog(true, baseLayer);
#else
        throw new NotImplementedException();
        string url = await MainPage.Current.DisplayPromptAsync("新增底图", "输入XYZ瓦片地图链接", "确定", "取消");
        if (!string.IsNullOrEmpty(url))
        {
            (BindingContext as BaseLayerViewViewModel).BaseLayers.Add(new Model.BaseLayerInfo()
            {
                Path = url,
                Type = Model.BaseLayerType.WebTiledLayer
            });
        }
#endif
        }
    }

    private void ShowAndroidDialog(bool modify, BaseLayerInfo modifyingBaseLayer = null)
    {
        var layout = new LinearLayoutCompat(Platform.CurrentActivity);
        layout.Orientation = LinearLayoutCompat.Vertical;
        layout.SetPadding(8, 8, 8, 8);

        var nameEdit = new TextInputEditText(Platform.CurrentActivity);
        nameEdit.SetSingleLine();
        nameEdit.Hint = "图层名";
        if (modify)
        {
            nameEdit.Text = modifyingBaseLayer.Name;
        }
        layout.AddView(nameEdit);

        var urlEdit = new TextInputEditText(Platform.CurrentActivity);
        urlEdit.SetSingleLine(false);
        urlEdit.SetMaxLines(3);
        urlEdit.Hint = "瓦片图层地址";
        if (modify)
        {
            urlEdit.Text = modifyingBaseLayer.Path;
        }
        layout.AddView(urlEdit);

        new AlertDialog.Builder(Platform.CurrentActivity)
             .SetTitle("新增底图")
             .SetView(layout)
             .SetNegativeButton("取消", (IDialogInterfaceOnClickListener)null)
             .SetPositiveButton("确定", new EventHandler<DialogClickEventArgs>(async (s, e) =>
             {
                 var name = nameEdit.Text;
                 var url = urlEdit.Text;
                 if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(name))
                 {
                     if ((BindingContext as BaseLayerViewViewModel).BaseLayers.Any(p => p.Name == name)
                     && (!modify || modifyingBaseLayer.Name != name))
                     {
                         await MainPage.Current.DisplayAlert("添加失败", "该名称的底图已存在", "确定");
                         return;
                     }
                     if (modify)
                     {
                         await ModifyLayerAsync(modifyingBaseLayer, name, url);
                     }
                     else
                     {
                         await AddLayerAsync(name, url);
                     }
                 }
             }))
             .SetCancelable(true)
             .Create()
             .Show();
    }
#endif
}