namespace HexControl.Core.Observable;

public class PropertyChangedEventArgs : EventArgs
{
    public PropertyChangedEventArgs() { }


    public PropertyChangedEventArgs(string? property)
    {
        Property = property;
    }

    public string? Property { get; }
}