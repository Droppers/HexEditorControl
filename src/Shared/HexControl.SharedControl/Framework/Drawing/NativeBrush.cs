namespace HexControl.SharedControl.Framework.Drawing;

internal abstract class NativeBrush<TNative> : ISharedBrush, IEquatable<NativeBrush<TNative>> where TNative : class
{
    protected NativeBrush(TNative brush)
    {
        Brush = brush;
    }

    public TNative Brush { get; }

    public bool Equals(NativeBrush<TNative>? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || other.Brush.Equals(Brush);
    }

    public override bool Equals(object? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Equals(other as NativeBrush<TNative>);
    }


    public override int GetHashCode() => Brush.GetHashCode();
}