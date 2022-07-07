namespace HexControl.SharedControl.Documents.Helpers;

public readonly struct IntegerColor : IEquatable<IntegerColor>
{
    public static readonly IntegerColor Zero = new(-1);

    public IntegerColor(int color)
    {
        Color = color;
    }

    public IntegerColor(byte r, byte g, byte b) : this(255, r, g, b) { }

    public IntegerColor(byte a, byte r, byte g, byte b)
    {
        Color = (a << 24) | (r << 16) | (g << 8) | b;
    }

    public int Color { get; }

    public byte A => (byte)((Color >> 24) & 255);
    public byte R => (byte)((Color >> 16) & 255);
    public byte G => (byte)((Color >> 8) & 255);
    public byte B => (byte)((Color >> 0) & 255);

    public bool Equals(IntegerColor other) => Color == other.Color;

    public override bool Equals(object? obj) => obj is IntegerColor other && Equals(other);

    public override int GetHashCode() => Color;
}
