namespace HexControl.Framework.Drawing;

internal abstract class NativePen<TNative> : ISharedPen where TNative : class
{
    protected NativePen(TNative pen)
    {
        Pen = pen;
    }

    public TNative Pen { get; }

    public bool Equals(ISharedPen? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is NativePen<TNative> otherNative && otherNative.Pen.Equals(Pen);
    }

    public abstract double Thickness { get; }

    public override bool Equals(object? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Equals(other as NativeBrush<TNative>);
    }


    public override int GetHashCode() => Pen.GetHashCode();
}