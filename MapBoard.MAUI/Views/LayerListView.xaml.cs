using Esri.ArcGISRuntime.Mapping;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.ViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MapBoard.Views;

public partial class LayerListView : ContentView
{
    public LayerListView()
    {
        InitializeComponent();

    }

    private void ContentView_Loaded(object sender, EventArgs e)
    {
        if (BindingContext != null)
        {
            return;
        }
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
        BindingContext = new LayerViewViewModel()
        {
            Layers = layers
        };
        (BindingContext as LayerViewViewModel).Update();
        if (Config.Instance.LastLayerListGroupType == 0)
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
        if (group)
        {
            lvwLevel.IsGroupingEnabled = true;
            lvwLevel.SetBinding(ListView.ItemsSourceProperty, nameof(LayerViewViewModel.Groups));
        }
        else
        {
            lvwLevel.IsGroupingEnabled = false;
            lvwLevel.SetBinding(ListView.ItemsSourceProperty, nameof(LayerViewViewModel.Layers));
        }
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
            int id = sender == rbtnByGroup ? 1 : 0;
            if (Config.Instance.LastLayerListGroupType != id)
            {
                Config.Instance.LastLayerListGroupType = id;
            }

        }
    }
}