using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.SharedControl.Framework.Host;

internal abstract class NativeFactory
{
    public abstract ISharedBrush WrapBrush(object brush);
    public abstract ISharedPen WrapPen(object pen);

    public abstract TNative ConvertObjectToNative<TNative>(object sharedObject);
}

internal abstract class NativeFactory<TBrush, TPen> : NativeFactory
{
    public override ISharedBrush WrapBrush(object brush) => WrapBrush((TBrush)brush);
    public override ISharedPen WrapPen(object pen) => WrapPen((TPen)pen);

    public abstract ISharedBrush WrapBrush(TBrush brush);
    public abstract ISharedPen WrapPen(TPen pen);

    public override TNative ConvertObjectToNative<TNative>(object sharedObject)
    {
        object obj = sharedObject switch
        {
            ISharedBrush brush => ConvertBrushToNative(brush)!,
            ISharedPen pen => ConvertPenToNative(pen)!,
            _ => throw new ArgumentOutOfRangeException(nameof(sharedObject), sharedObject, null)
        };

        return (TNative)obj;
    }

    public abstract TBrush ConvertBrushToNative(ISharedBrush brush);
    public abstract TPen ConvertPenToNative(ISharedPen pen);
}