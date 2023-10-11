using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class TrackingBar : ContentView
{
	public TrackingBar()
	{
		InitializeComponent();
		BindingContext = new TrackViewViewModel();
	}
}