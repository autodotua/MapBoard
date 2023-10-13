public class SidePanelInfo
{
    public Type Type { get; set; }
    public VisualElement Container { get; set; }
    public ISidePanel Content { get; set; }
    public SwipeDirection Direction { get; set; }
    public int Length { get; set; }
    public bool Standalone { get; set; }
    public bool IsOpened { get; set; }
}