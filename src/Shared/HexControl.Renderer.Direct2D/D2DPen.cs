using HexControl.SharedControl.Framework.Drawing;
using SharpDX.Direct2D1;

namespace HexControl.Renderer.Direct2D;

internal record D2DPen(SolidColorBrush Brush, double Thickness, PenStyle Style);