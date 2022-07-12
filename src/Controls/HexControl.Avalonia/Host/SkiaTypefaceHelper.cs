using System.Collections.Concurrent;
using System.Reflection;
using Avalonia.Media;

namespace HexControl.Avalonia.Host;

internal static class SkiaTypefaceHelper
{
    private static readonly ConcurrentDictionary<(GlyphTypeface, double), TypefaceDetails?> Cache = new();

    public static TypefaceDetails? GetTypefaceDetails(GlyphTypeface glyphTypeface, double size)
    {
        // Hack to determine CapHeight for a Skia Typeface, which translates to:
        // var skFont = glyphTypeface.Typeface.ToFont()
        // var oldSize = skFont.Size;
        // skFont.Size = size;
        // skFont.GetFontMetrics(out SKFontMetrics metrics);
        // var fontOffset = Math.Floor(metrics.Top - metrics.Ascent);
        // var capHeight = Math.Ceiling(metrics.CapHeight);
        // skFont.Size = oldSize;

        if (Cache.TryGetValue((glyphTypeface, size), out var details))
        {
            return details;
        }

        var impl = glyphTypeface.PlatformImpl;
        var type = impl.GetType();
        if (type.FullName != "Avalonia.Skia.GlyphTypefaceImpl")
        {
            return null;
        }

        var typefaceProperty = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(s => s.Name == "Typeface");

        if (typefaceProperty is null)
        {
            return null;
        }

        var typeface = typefaceProperty.GetValue(impl, null)!;
        var typefaceMethods = typeface.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

        var toFontMethod = typefaceMethods.First(m => m.Name == "ToFont" && m.GetParameters().Length == 0);
        var font = toFontMethod.Invoke(typeface, null)!;
        var fontProperties = font.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var fontMethods = font.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

        var sizeProp = fontProperties.First(s => s.Name == "Size");
        var oldSize = sizeProp.GetValue(font, null);

        try
        {
            sizeProp.SetValue(font, (float)size);

            var getFontMetricsMethod = fontMethods.First(m => m.Name == "GetFontMetrics");

            var args = new object?[] { null };
            getFontMetricsMethod.Invoke(font, args);
            var metrics = args[0]!;
            var metricProperties = metrics.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var capHeightProperty = metricProperties.First(m => m.Name == "CapHeight");

            var topProperty = metricProperties.First(m => m.Name == "Top");
            var top = topProperty.GetValue(metrics, null)!;
            var ascentProperty = metricProperties.First(m => m.Name == "Ascent");
            var ascent = ascentProperty.GetValue(metrics, null)!;

            var topOffset = Math.Floor((float)top - (float)ascent);
            var capHeight = capHeightProperty.GetValue(metrics, null)!;

            details = new TypefaceDetails(Math.Ceiling((float)capHeight), topOffset);
            _ = Cache.TryAdd((glyphTypeface, size), details);
            return details;
        }
        finally
        {
            sizeProp.SetValue(font, oldSize);
        }
    }

    public record struct TypefaceDetails(double CapHeight, double TopOffset);
}