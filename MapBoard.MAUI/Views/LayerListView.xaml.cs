using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class LayerListView : ContentView
{
    public LayerListView()
    {
        InitializeComponent();
    }

    public void Initialize()
    {
        BindingContext = new LayerViewViewModel()
        {
            Layers = MainMapView.Current.Layers
        };
        (BindingContext as LayerViewViewModel).GenerateGroups();
        if (Config.Instance.LastLayerListGroupType == 0)
        {
            rbtnByLevel.IsChecked = true;
        }
        else
        {
            rbtnByGroup.IsChecked = true;
        }
    }

    private void ViewTypeRadioButton_CheckChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            if (sender == rbtnByGroup)
            {
                lvwGroup.IsVisible = true;
                lvwLevel.IsVisible = false;
            }
            else
            {
                lvwGroup.IsVisible = false;
                lvwLevel.IsVisible = true;
            }
        }
    }

    private void LvwGroup_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        var layer = e.Item as MapLayerInfo;
        layer.LayerVisible = !layer.LayerVisible;
    }
}