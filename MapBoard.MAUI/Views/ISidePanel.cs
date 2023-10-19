public interface ISidePanel
{
    public void OnPanelOpening();
    public void OnPanelClosed();
    public SwipeDirection Direction { get; }
    public int Length { get; }
    public bool Standalone { get; }
}