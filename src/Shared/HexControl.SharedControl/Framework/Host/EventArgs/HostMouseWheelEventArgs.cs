using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.SharedControl.Framework.Host.EventArgs;

internal class HostMouseWheelEventArgs : PointerEventArgs
{
    public HostMouseWheelEventArgs(SharedPoint point, int delta) : base(point)
    {
        Delta = delta;
    }

    public int Delta { get; }
}