namespace HexControl.Framework.Drawing;

internal abstract class NativeBrush<TNative> : ISharedBrush where TNative : notnull
{
    protected NativeBrush(TNative brush)
    {
        Brush = brush;
    }

    public TNative Brush { get; }

    public bool Equals(ISharedBrush? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is NativeBrush<TNative> otherNativeBrush && otherNativeBrush.Brush.Equals(Brush);
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