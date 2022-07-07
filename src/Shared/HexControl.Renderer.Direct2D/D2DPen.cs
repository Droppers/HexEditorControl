using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct2D1;

namespace HexControl.Renderer.Direct2D;

internal record D2DPen(SolidColorBrush Brush, double Thickness, PenStyle Style) : IDisposable
{
    public void Dispose()
    {
        Brush.Dispose();
    }

    public virtual bool Equals(D2DPen? other) => other is not null &&
                                                 other.Brush.GetHashCode() == Brush.GetHashCode() &&
                                                 Math.Abs(other.Thickness - Thickness) < double.Epsilon &&
                                                 other.Style == Style;

    public override int GetHashCode() => HashCode.Combine(Brush.GetHashCode(), Thickness, Style);
}