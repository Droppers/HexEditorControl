using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.SharedControl.Framework.Host.EventArgs;

internal class HostMouseButtonEventArgs : PointerEventArgs
{
    public HostMouseButtonEventArgs(HostMouseButton button, SharedPoint point) : base(point)
    {
        Button = button;
    }

    public HostMouseButton Button { get; }
}