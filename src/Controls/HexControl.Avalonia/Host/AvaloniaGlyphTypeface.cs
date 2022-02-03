using System;
using Avalonia.Media;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Host.Typeface;
using TextAlignment = HexControl.SharedControl.Framework.Drawing.Text.TextAlignment;

namespace HexControl.Avalonia.Host;

internal class AvaloniaGlyphTypeface : CachedGlyphTypeface<GlyphTypeface>
{
    public AvaloniaGlyphTypeface(string typefaceName)
    {
        RegularTypeface = new Typeface(typefaceName);
        Typeface = new GlyphTypeface(RegularTypeface);
    }

    public AvaloniaGlyphTypeface(EmbeddedAsset asset)
    {
        RegularTypeface = new Typeface($"resm:{asset.Assembly}.{asset.File}?assembly={asset.Assembly}#{asset.Name}");
        Typeface = new GlyphTypeface(RegularTypeface);
    }

    public Typeface RegularTypeface { get; }
    public override GlyphTypeface Typeface { get; }

    public override double GetWidth(double size)
    {
        var scale = size / Typeface.DesignEmHeight;
        return Typeface.GetGlyphAdvance('W') * scale;
    }

    public override double GetHeight(double size)
    {
        var scale = size / Typeface.DesignEmHeight;
        return Math.Abs(Typeface.Ascent + Typeface.Descent) * scale;
    }

    public override double GetGlyphOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException();
        }


        var scale = size / Typeface.DesignEmHeight;
        return -(Typeface.Descent * scale);
    }

    public override bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex) =>
        Typeface.TryGetGlyph((uint)codePoint, out glyphIndex);
}