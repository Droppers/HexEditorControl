using System;
using SharpDX.Direct2D1;

namespace HexControl.Renderer.Direct2D;

internal static class SharpDxExtensions
{
    public static int GetHashCode(this SolidColorBrush brush) =>
        HashCode.Combine(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
}