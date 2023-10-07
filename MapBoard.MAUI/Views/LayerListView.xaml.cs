using MapBoard.Mapping;
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
}