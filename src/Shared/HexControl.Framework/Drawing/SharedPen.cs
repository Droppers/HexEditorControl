namespace HexControl.Framework.Drawing;

public class SharedPen : ISharedPen 
{
    public SharedPen(ISharedBrush brush, double thickness, PenStyle style = PenStyle.Solid)
    {
        Brush = brush;
        Thickness = thickness;
        Style = style;
    }

    public PenStyle Style { get; }

    public ISharedBrush Brush { get; }

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

        return other is SharedPen otherSharedPen && Brush.Equals(otherSharedPen.Brush) && Thickness.Equals(otherSharedPen.Thickness) && Style.Equals(otherSharedPen.Style);
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