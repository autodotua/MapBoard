using Esri.ArcGISRuntime.Mapping;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.ViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MapBoard.Views;

public partial class LayerListView : ContentView, ISidePanel
{
    private bool isLoaded = false;

    public LayerListView()
    {
        InitializeComponent();
        BindingContext = new LayerViewViewModel();

    }
    public void OnPanelClosed()
    {
    }

    public void OnPanelOpening()
    {
    }

    private void ContentView_Loaded(object sender, EventArgs e)
    {
        if (isLoaded)
        {
            return;
        }
        isLoaded = true;
        MainMapView.Current.MapLoaded += MapView_MapLoaded;
        MainMapView.Current.Layers.CollectionChanged += Layers_CollectionChanged;
        MainMapView.Current.Layers.PropertyChanged += Layers_PropertyChanged;
        MapView_MapLoaded(sender, e);
    }

    private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (!MainMapView.Current.Layers.IsBatchLoading)
        {
            (BindingContext as LayerViewViewModel).Update();
        }
    }

    private void Layers_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        //绑定有点问题，只能手动同步Selected
        if (e.PropertyName == nameof(MapLayerCollection.Selected))
        {
            UpdateListSelectedItem();
        }
    }

    private void lvwGroup_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItemIndex >= 0)
        {
            MainMapView.Current.Layers.Selected = e.SelectedItem as IMapLayerInfo;
        }
    }

    private void MapView_MapLoaded(object sender, EventArgs e)
    {
        var layers = MainMapView.Current.Layers;
        (BindingContext as LayerViewViewModel).Layers = layers;
        (BindingContext as LayerViewViewModel).Update();
        if (false && Config.Instance.LastLayerListGroupType == 0)
        {
            rbtnByLevel.IsChecked = true;
        }
        else
        {
            rbtnByGroup.IsChecked = true;
        }
        UpdateListSelectedItem();
    }

    private void UpdateListSelectedItem()
    {
        lvwLevel.SelectedItem = null;
        lvwLevel.SelectedItem = MainMapView.Current.Layers.Selected;
    }

    private void UpdateListType(bool group)
    {
        //存在渲染问题
        //https://github.com/dotnet/maui/issues/16031
        lvwLevel.BeginRefresh();
        lvwLevel.RemoveBinding(ListView.ItemsSourceProperty);
        lvwLevel.SetBinding(ListView.ItemsSourceProperty, group ? nameof(LayerViewViewModel.Groups) : nameof(LayerViewViewModel.Layers));
        lvwLevel.IsGroupingEnabled = group;
        lvwLevel.EndRefresh();
        UpdateListSelectedItem();
        if (lvwLevel.SelectedItem != null)
        {
            lvwLevel.ScrollTo(lvwLevel.SelectedItem, ScrollToPosition.MakeVisible, true);
        }
    }

    private void ViewTypeRadioButton_CheckChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            UpdateListType(sender == rbtnByGroup);
            UpdateListType(sender != rbtnByGroup);
            UpdateListType(sender == rbtnByGroup);
            int id = sender == rbtnByGroup ? 1 : 0;
            if (Config.Instance.LastLayerListGroupType != id)
            {
                Config.Instance.LastLayerListGroupType = id;
            }

        }
    }
}