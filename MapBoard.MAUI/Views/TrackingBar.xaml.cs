using MapBoard.ViewModels;

namespace MapBoard.Views;

public partial class TrackingBar : ContentView, ISidePanel
{
    public TrackingBar()
    {
        InitializeComponent();
        BindingContext = new TrackViewViewModel();
    }

    public SwipeDirection Direction => SwipeDirection.Up;

    public int Length => 96;

    public bool Standalone => true;
    public void OnPanelClosed()
    {
    }

    public void OnPanelOpening()
    {
    }
}