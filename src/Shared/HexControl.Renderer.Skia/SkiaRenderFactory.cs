using System.Drawing;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Drawing;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Host.Typeface;
using SkiaSharp;

namespace HexControl.Renderer.Skia;

internal abstract class SkiaRenderFactory : RenderFactory<SKPaint, SKPaint>
{
    public override IGlyphTypeface CreateGlyphTypeface(string fontFamily) => new SkiaGlyphTypeface(fontFamily);

    public override IGlyphTypeface CreateGlyphTypeface(EmbeddedAsset asset) => throw new NotImplementedException();

    public override SKPaint CreateBrush(ISharedBrush brush)
    {
        if (brush is ColorBrush colorBrush)
        {
            var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.Style = SKPaintStyle.Fill;
            paint.Color = Convert(colorBrush.Color);
            return paint;
        }

        throw new NotImplementedException($"Shared brush of type {brush.GetType().Name} has not yet been implemented.");
    }

    public override SKPaint CreatePen(ISharedPen pen)
    {
        if (pen is SharedPen sharedPen)
        {
            var paint = CreateBrush(sharedPen.Brush);
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = (float)sharedPen.Thickness;
            paint.PathEffect = Convert(sharedPen.Style);
            return paint;
        }
        throw new NotSupportedException($"Shared pen of type {pen.GetType().Name} has not yet been implemented.");
    }

    private static SKColor Convert(Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    private static SKPathEffect? Convert(PenStyle style)
    {
        return style switch
        {
            PenStyle.Dashed => null,
            PenStyle.Dotted => null,
            PenStyle.Solid => null,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }
}