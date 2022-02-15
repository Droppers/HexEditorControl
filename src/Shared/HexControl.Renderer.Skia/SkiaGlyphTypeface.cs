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

    public override double GetCapHeight(double size)
    {
        Typeface.Size = (float)size;
        Typeface.GetFontMetrics(out var metrics);
        return Math.Ceiling(metrics.CapHeight);
    }

    public override double GetWidth(double size)
    {
        Typeface.Size = (float)size;
        var widths = new float[1];
        var bounds = new SKRect[1];
        Typeface.GetGlyphWidths(new ushort[] {'W'}, widths, bounds);
        return widths[0];
    }

    public override bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex)
    {
        glyphIndex = Typeface.GetGlyph(codePoint);
        return true;
    }

    public override double GetGlyphOffsetY(TextAlignment alignment, double size) => GetCapHeight(size);
    
    public override double GetTextOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException($"TextAlignment {alignment} is not yet supported.");
        }

        return GetCapHeight(size);
    }
}