using HexControl.Framework.Drawing;

namespace HexControl.Framework.Host.Events;

internal class HostMouseWheelEventArgs : PointerEventArgs
{
    public HostMouseWheelEventArgs(SharedPoint point, int delta) : base(point)
    {
        Delta = delta;
    }

    public int Delta { get; }
}