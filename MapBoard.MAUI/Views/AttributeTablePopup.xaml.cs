using CommunityToolkit.Maui.Views;
using Esri.ArcGISRuntime.Data;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class AttributeTablePopup : Popup
{
    public AttributeTablePopup(Feature feature)
    {
        var layerInfo = MainMapView.Current.Layers.FindLayer(feature.FeatureTable.Layer);
        if (layerInfo == null)
        {
            throw new Exception("找不到feature对应的MapLayerInfo");
        }
        BindingContext = new AttributeTablePopupViewModel()
        {
            Attributes = FeatureAttributeCollection.FromFeature(layerInfo, feature)
        };
        InitializeComponent();
        Feature = feature;
    }

    public Feature Feature { get; }

    private void ApplyButton_Clicked(object sender, EventArgs e)
    {
        (BindingContext as AttributeTablePopupViewModel).Attributes.SaveToFeature();
        Close();
    }

    private void CancelButton_Clicked(object sender, EventArgs e)
    {
        Close();
    }
}