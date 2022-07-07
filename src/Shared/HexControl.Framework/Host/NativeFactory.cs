using HexControl.Framework.Drawing;

namespace HexControl.Framework.Host;

internal abstract class NativeFactory<TBrush, TPen> : INativeFactory
    where TBrush : notnull
    where TPen : notnull
{
    public ISharedBrush WrapBrush(object brush) => WrapBrush((TBrush)brush);
    public ISharedPen WrapPen(object pen) => WrapPen((TPen)pen);

    public object UnwrapBrush(object brush) => ConvertBrushToNative((ISharedBrush)brush);
    public object UnwrapPen(object pen) => ConvertPenToNative((ISharedPen)pen);

    public abstract ISharedBrush WrapBrush(TBrush brush);
    public abstract ISharedPen WrapPen(TPen pen);

    public abstract TBrush ConvertBrushToNative(ISharedBrush brush);
    public abstract TPen ConvertPenToNative(ISharedPen pen);
}