using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class TrackingBar : ContentView, ISidePanel
{
	public TrackingBar()
	{
		InitializeComponent();
		BindingContext = new TrackViewViewModel();
	}

    public void OnPanelClosed()
    {
    }

    public void OnPanelOpening()
    {
    }
}