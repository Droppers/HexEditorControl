using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.SharedControl.Framework.Host;

internal interface INativeFactory
{
    public ISharedBrush WrapBrush(object brush);
    public ISharedPen WrapPen(object pen);

    public object UnwrapBrush(object brush);
    public object UnwrapPen(object pen);
}