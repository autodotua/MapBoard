public class SidePanelInfo
{
    public SidePanelInfo(VisualElement container, ISidePanel content)
    {
        Container = container;
        Content = content;
        Type = Content.GetType();
        Direction = Content.Direction;
        Length = Content.Length;
        Standalone = Content.Standalone;
    }
    public SidePanelInfo()
    {
    }

    public Type Type { get; set; }
    public VisualElement Container { get; set; }
    public ISidePanel Content { get; set; }
    public SwipeDirection Direction { get; set; }
    public int Length { get; set; }
    public bool Standalone { get; set; }
    public bool IsOpened { get; set; }
}