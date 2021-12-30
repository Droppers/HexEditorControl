namespace HexControl.SharedControl.Framework.Host.Controls;

internal class HostScrollEventArgs : System.EventArgs
{
    public HostScrollEventArgs(double newValue, HostScrollEventType scrollType)
    {
        NewValue = newValue;
        ScrollType = scrollType;
    }

    public double NewValue { get; }
    public HostScrollEventType ScrollType { get; }
}

internal enum HostScrollEventType
{
    EndScroll,
    First,
    LargeDecrement,
    LargeIncrement,
    Last,
    SmallDecrement,
    SmallIncrement,
    ThumbPosition,
    ThumbTrack
}

internal interface IHostScrollBar : IHostControl
{
    public double Value { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; }

    public double Viewport { get; set; }

    public bool AutoHide { get; }
    public event EventHandler<HostScrollEventArgs>? Scroll;
}