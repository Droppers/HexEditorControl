using System.Drawing;

namespace HexControl.Framework.Drawing;

public class ColorBrush : ISharedBrush, IEquatable<ColorBrush>
{
    public ColorBrush(Color color)
    {
        Color = color;
    }

    public Color Color { get; }

    public bool Equals(ColorBrush? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Color.Equals(other.Color);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is ColorBrush other && Equals(other);
    }

    public override int GetHashCode() => Color.GetHashCode();
}