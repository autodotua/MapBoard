using CommunityToolkit.Maui.Views;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.Util;

namespace MapBoard.Views;

public partial class LayerStylePopup : Popup
{
	public LayerStylePopup(IMapLayerInfo layer)
	{
		InitializeComponent();
        RawLayer = layer;
        Layer = layer.Clone() as IMapLayerInfo;
        BindingContext = Layer;
    }

    public IMapLayerInfo Layer { get; }
    public IMapLayerInfo RawLayer { get; }
    private void ApplyButton_Clicked(object sender, EventArgs e)
    {
        RawLayer.Display = Layer.Display;
        RawLayer.Renderer = Layer.Renderer;
        RawLayer.Labels = Layer.Labels;
        RawLayer.Interaction = Layer.Interaction;
        RawLayer.ApplyStyle();
        Close();
    }

    private void CalcelButton_Clicked(object sender, EventArgs e)
    {
        Close();
    }
    private async void DeleteButton_Click(object sender, EventArgs e)
    {
        if(await MainPage.Current.DisplayAlert("移除图层","是否移除选择的图层？","确定","取消"))
        {
            MainMapView.Current.Layers.Remove(RawLayer);
            Close();
        }
    }
}