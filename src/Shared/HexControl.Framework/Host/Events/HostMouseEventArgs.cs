using HexControl.Framework.Drawing;

namespace HexControl.Framework.Host.Events;

internal class HostMouseEventArgs : PointerEventArgs
{
    public HostMouseEventArgs(SharedPoint point) : base(point) { }
}