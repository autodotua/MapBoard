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
using Switch = Microsoft.Maui.Controls.Switch;
using MapBoard.Util;
using System.Reflection.Metadata;
using static MapBoard.Mapping.CacheableWebTiledLayer;
using Microsoft.EntityFrameworkCore;

namespace MapBoard.Views;

public partial class BaseLayerView : ContentView, ISidePanel
{
    public SwipeDirection Direction => SwipeDirection.Left;

    public int Length => 300;

    public bool Standalone => false;

    public BaseLayerView()
    {
        InitializeComponent();
    }

    public void OnPanelClosed()
    {
        (BindingContext as BaseLayerViewViewModel).Save();
    }

    public void OnPanelOpening()
    {
        BindingContext = new BaseLayerViewViewModel();
    }

    private void AddNewBaseLayerButton_Clicked(object sender, EventArgs e)
    {
#if ANDROID
        ShowAndroidDialog(false);
#else
        throw new NotImplementedException();
#endif
    }
#if ANDROID
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

    private async Task AddLayerAsync(string name, string url)
    {
        var handle = ProgressPopup.Show("正在添加底图");
        try
        {
            var baseLayer = new BaseLayerInfo()
            {
                Name = name,
                Path = url,
                Type = BaseLayerType.WebTiledLayer
            };
            (BindingContext as BaseLayerViewViewModel).BaseLayers.Insert(0, baseLayer);
            await MapViewHelper.AddLayerAsync(MainMapView.Current.Map.Basemap, baseLayer, true);
        }
        catch (Exception ex)
        {
            await MainPage.Current.DisplayAlert("添加失败", ex.Message, "确定");
        }
        finally
        {
            handle.Close();
        }
    }

    private void ContentView_Loaded(object sender, EventArgs e)
    {

    }


    private async Task ModifyLayerAsync(BaseLayerInfo baseLayer, string name, string url)
    {
        var handle = ProgressPopup.Show("正在修改底图");
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
            handle.Close();
        }
    }


    private void LayerVisibleSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        var view = sender as Switch;
        if (view.BindingContext is BaseLayerInfo baseLayer)
        {
            var esriBaseLayer = MainMapView.Current.Map.Basemap.BaseLayers.FirstOrDefault(p => p.Name == baseLayer.Name);
            if (esriBaseLayer != null)
            {
                esriBaseLayer.IsVisible = baseLayer.Visible;
            }
        }

    }

    private async void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        var baseLayer = e.Item as BaseLayerInfo;

        string deleteText = "删除";
        string modifyText = "修改";
        var result = await MainPage.Current.DisplayActionSheet("底图操作", "取消", null, deleteText, modifyText);
        if (result == deleteText)
        {
            (BindingContext as BaseLayerViewViewModel).BaseLayers.Remove(baseLayer);
            var esriBaseLayer = MainMapView.Current.Map.Basemap.BaseLayers.First(p => p.Name == baseLayer.Name);
            MainMapView.Current.Map.Basemap.BaseLayers.Remove(esriBaseLayer);
        }
        else if (result == modifyText)
        {
#if ANDROID
            ShowAndroidDialog(true, baseLayer);
#else
        throw new NotImplementedException();
#endif
        }
    }

    private async void ClearCache_Tapped(object sender, TappedEventArgs e)
    {
        if (await MainPage.Current.DisplayAlert("清除缓存", "是否清除所有底图缓存？", "是", "否"))
        {
            CacheableWebTiledLayerDbContext db = new CacheableWebTiledLayerDbContext();
            await db.Database.ExecuteSqlRawAsync("delete from Tiles");
            await MainPage.Current.DisplayAlert("清除缓存", "缓存已清空？", "关闭");
        }

    }
}