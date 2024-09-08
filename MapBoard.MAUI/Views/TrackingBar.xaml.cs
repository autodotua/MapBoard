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

    public void SetStatusBarHeight(double height)
    {
        mainLayout.Margin = new Thickness(8, 8 + height, 8, 8);
    }

    private void ContentView_Loaded(object sender, EventArgs e)
    {
#if ANDROID
        SetStatusBarHeight((Platform.CurrentActivity as MainActivity).GetStatusBarHeight());
#endif
    }
}