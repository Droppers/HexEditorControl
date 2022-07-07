using HexControl.Framework.Drawing;

namespace HexControl.Framework.Host.Events;

internal class HostMouseButtonEventArgs : PointerEventArgs
{
    public HostMouseButtonEventArgs(HostMouseButton button, SharedPoint point) : base(point)
    {
        Button = button;
    }

    public HostMouseButton Button { get; }
}