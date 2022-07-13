namespace HexControl.Framework.Drawing;

internal abstract class NativePen<TNative> : ISharedPen, IEquatable<NativePen<TNative>> where TNative : class
{
    protected NativePen(TNative pen)
    {
        Pen = pen;
    }

    public TNative Pen { get; }

    public bool Equals(NativePen<TNative>? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || other.Pen.Equals(Pen);
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