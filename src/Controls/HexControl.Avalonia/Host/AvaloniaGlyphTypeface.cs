using System.Reflection;
using Avalonia.Media;
using HexControl.Framework;
using HexControl.Framework.Host.Typeface;
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

    private (double? capHeight, double? topOffset) GetSkiaCapHeight(double size)
    {
        // TODO
        //  - Move to static class
        //  - Cache information
        //  - Don't use single, add extra null checks.
        //  - Support for direct2d1 implementation in Avalonia(?)
        //  - Verify if implementation is "Avalonia.Skia.GlyphTypefaceImpl"

        // Hack to determine CapHeight for a Skia Typeface
        // var skFont = {Typeface}.Typeface.ToFont()
        // var oldSize = skFont.Size;
        // skFont.Size = {size};
        // skFont.GetFontMetrics(out SKFontMetrics metrics);
        // var fontOffset = Math.Floor(metrics.Top - metrics.Ascent);
        // var capHeight = Math.Ceiling(metrics.CapHeight);
        // skFont.Size = oldSize;

        var impl = Typeface.PlatformImpl;
        var type = impl.GetType();
        var typefaceProperty = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(s => s.Name == "Typeface");

        if (typefaceProperty is null)
        {
            return (null, null);
        }

        var typeface = typefaceProperty.GetValue(impl, null)!;
        var typefaceMethods = typeface.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

        var toFontMethod = typefaceMethods.Single(m => m.Name == "ToFont" && m.GetParameters().Length == 0);
        var font = toFontMethod.Invoke(typeface, null)!;
        var fontProperties = font.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var fontMethods = font.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

        var sizeProp = fontProperties.Single(s => s.Name == "Size");
        var oldSize = sizeProp.GetValue(font, null);

        try
        {
            sizeProp.SetValue(font, (float)size);

            var getFontMetricsMethod = fontMethods.Single(m => m.Name == "GetFontMetrics");

            var args = new object?[] {null};
            getFontMetricsMethod.Invoke(font, args);
            var metrics = args[0]!;
            var metricProperties = metrics.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var capHeightProperty = metricProperties.Single(m => m.Name == "CapHeight");

            var topProperty = metricProperties.Single(m => m.Name == "Top");
            var top = topProperty.GetValue(metrics, null)!;
            var ascentProperty = metricProperties.Single(m => m.Name == "Ascent");
            var ascent = ascentProperty.GetValue(metrics, null)!;

            var topOffset = Math.Floor((float)top - (float)ascent);
            var capHeight = capHeightProperty.GetValue(metrics, null)!;

            return (Math.Ceiling((float)capHeight), topOffset);
        }
        finally
        {
            sizeProp.SetValue(font, oldSize);
        }
    }

    public override double GetWidth(double size)
    {
        var scale = size / Typeface.DesignEmHeight;
        return Typeface.GetGlyphAdvance('W') * scale;
    }

    public override double GetCapHeight(double size)
    {
        var skiaCapHeight = GetSkiaCapHeight(size);
        if (skiaCapHeight.capHeight.HasValue)
        {
            return skiaCapHeight.capHeight.Value;
        }

        throw new InvalidOperationException("Could not determine font metrics for Avalonia typeface.");
    }

    public override double GetGlyphOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException();
        }

        var fontMetrics = GetSkiaCapHeight(size);
        if (fontMetrics.topOffset.HasValue)
        {
            return fontMetrics.topOffset.Value;
        }

        throw new InvalidOperationException("Could not determine font metrics for Avalonia typeface.");
    }

    public override bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex) =>
        Typeface.TryGetGlyph((uint)codePoint, out glyphIndex);
}