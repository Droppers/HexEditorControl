using HexControl.SharedControl.Framework.Drawing.Text;
using HexControl.SharedControl.Framework.Host.Typeface;
using SkiaSharp;

namespace HexControl.Renderer.Skia;

internal sealed class SkiaGlyphTypeface : CachedGlyphTypeface<SKFont>
{
    public SkiaGlyphTypeface(string fontFamily)
    {
        var glyphTypeface = SKTypeface.FromFamilyName(fontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright);
        Typeface = glyphTypeface.ToFont();
        Typeface.Edging = SKFontEdging.SubpixelAntialias;
        Typeface.Subpixel = true;
    }

    public override SKFont Typeface { get; }

    public override double GetHeight(double size)
    {
        Typeface.Size = (float)size;
        Typeface.GetFontMetrics(out var metrics);
        return metrics.Bottom;
    }

    public override double GetWidth(double size)
    {
        Typeface.Size = (float)size;
        var widths = new float[1];
        var bounds = new SKRect[1];
        Typeface.GetGlyphWidths(new ushort[] {'B'}, widths, bounds);
        return widths[0];
    }

    public override bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex)
    {
        glyphIndex = Typeface.GetGlyph(codePoint);
        return true;
    }

    public override double GetGlyphOffsetY(TextAlignment alignment, double size) => GetHeight(size);
}