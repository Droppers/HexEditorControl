namespace HexControl.Framework.Drawing;

public interface ISharedPen : IEquatable<ISharedPen>
{
    public double Thickness { get; }
}