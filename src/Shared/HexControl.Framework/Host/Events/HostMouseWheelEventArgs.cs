using HexControl.Framework.Drawing;

namespace HexControl.Framework.Host.Events;

internal class HostMouseWheelEventArgs : PointerEventArgs
{
    public HostMouseWheelEventArgs(SharedPoint point, SharedPoint delta) : base(point)
    {
        Delta = delta;
    }

    public SharedPoint Delta { get; }
}