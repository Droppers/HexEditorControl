using System.Drawing;

namespace HexControl.Framework.Drawing;

public class ColorBrush : ISharedBrush
{
    public ColorBrush(Color color)
    {
        Color = color;
    }

    public Color Color { get; }

    public bool Equals(ISharedBrush? other)
    {
        if (!ReferenceEquals(this, other) || other is not ColorBrush otherColorBrush)
        {
            return false;
        }

        return Color.Equals(otherColorBrush.Color);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        
        return ReferenceEquals(this, obj) || obj is ColorBrush other && Equals(other);
    }

    public override int GetHashCode() => Color.GetHashCode();
}