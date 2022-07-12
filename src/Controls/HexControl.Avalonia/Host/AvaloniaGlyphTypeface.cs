using Avalonia.Media;
using HexControl.Framework;
using HexControl.Framework.Host;
using TextAlignment = HexControl.Framework.Drawing.Text.TextAlignment;

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

    public override double GetCapHeight(double size)
    {
        var details = SkiaTypefaceHelper.GetTypefaceDetails(Typeface, size);
        if (details.HasValue)
        {
            return details.Value.CapHeight;
        }

        throw new InvalidOperationException("Could not determine font metrics for Avalonia typeface.");
    }

    public override double GetGlyphOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException();
        }

        var details = SkiaTypefaceHelper.GetTypefaceDetails(Typeface, size);
        if (details.HasValue)
        {
            return details.Value.TopOffset;
        }

        throw new InvalidOperationException("Could not determine font metrics for Avalonia typeface.");
    }

    protected override bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex) =>
        Typeface.TryGetGlyph((uint)codePoint, out glyphIndex);
}