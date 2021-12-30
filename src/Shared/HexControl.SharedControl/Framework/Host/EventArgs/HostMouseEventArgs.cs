using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.SharedControl.Framework.Host.EventArgs;

internal class HostMouseEventArgs : PointerEventArgs
{
    public HostMouseEventArgs(SharedPoint point) : base(point) { }
}