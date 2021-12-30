namespace HexControl.Core.Events;

public class SelectionChangedEventArgs
{
    public SelectionChangedEventArgs(Selection? oldArea, Selection? newArea, bool requestCenter)
    {
        OldArea = oldArea;
        NewArea = newArea;
        RequestCenter = requestCenter;
    }

    public Selection? OldArea { get; }
    public Selection? NewArea { get; }
    public bool RequestCenter { get; }
}