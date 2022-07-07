namespace HexControl.Framework.Drawing;

public enum PenStyle
{
    Solid,
    Dashed,
    Dotted
}

public class SharedPen : ISharedPen, IEquatable<SharedPen>
{
    public SharedPen(ISharedBrush brush, double thickness, PenStyle style = PenStyle.Solid)
    {
        Brush = brush;
        Thickness = thickness;
        Style = style;
    }

    public PenStyle Style { get; }

    public ISharedBrush Brush { get; }

    public bool Equals(SharedPen? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Brush.Equals(other.Brush) && Thickness.Equals(other.Thickness) && Style.Equals(other.Style);
    }

    public double Thickness { get; }

    public override bool Equals(object? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Equals(other as SharedPen);
    }

    public override int GetHashCode() => HashCode.Combine(Brush, Thickness);
}