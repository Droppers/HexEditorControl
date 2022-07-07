namespace HexControl.Framework.Drawing;

public readonly record struct SharedRectangle(double X, double Y, double Width, double Height)
{
    public static SharedRectangle Union(SharedRectangle a, SharedRectangle b)
    {
        var x1 = Math.Min(a.X, b.X);
        var x2 = Math.Max(a.X + a.Width, b.X + b.Width);
        var y1 = Math.Min(a.Y, b.Y);
        var y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

        return new SharedRectangle(x1, y1, x2 - x1, y2 - y1);
    }
}