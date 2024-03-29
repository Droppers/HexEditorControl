﻿using System.Globalization;
using HexControl.Framework.Host;
using SharpDX.DirectWrite;
using TextAlignment = HexControl.Framework.Drawing.Text.TextAlignment;

namespace HexControl.Renderer.Direct2D;

internal class D2DGlyphTypeface : CachedGlyphTypeface<FontFace>
{
    private Factory _factory;

    private FontFace? _typeface;

    public D2DGlyphTypeface(string fontFamily)
    {
        FontFamily = fontFamily;
        _factory = new Factory();
        _typeface = GetFontFace(fontFamily);
    }

    public string FontFamily { get; }

    public override FontFace Typeface => _typeface!;

    private FontFace GetFontFace(string fontFamilyName)
    {
        using var fontCollection = _factory.GetSystemFontCollection(false);
        var familyCount = fontCollection.FontFamilyCount;
        for (var i = 0; i < familyCount; i++)
        {
            using var fontFamily = fontCollection.GetFontFamily(i);
            var familyNames = fontFamily.FamilyNames;

            if (!familyNames.FindLocaleName(CultureInfo.CurrentCulture.Name, out var index))
            {
                familyNames.FindLocaleName("en-us", out index);
            }

            var name = familyNames.GetString(index);
            if (!name.Equals(fontFamilyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fontCount = fontFamily.FontCount;
            for (var fontIndex = 0; fontIndex < fontCount; fontIndex++)
            {
                using var font = fontFamily.GetFont(fontIndex);
                if (font.Style != FontStyle.Normal && font.Weight != FontWeight.Regular)
                {
                    continue;
                }

                return new FontFace(font);
            }
        }

        throw new ArgumentOutOfRangeException(nameof(fontFamilyName),
            $"Could not find font family '{fontFamilyName}'.");
    }
    
    public override double GetCapHeight(double size)
    {
        var metrics = _typeface!.Metrics;
        var ratio = size / metrics.DesignUnitsPerEm;
        return metrics.CapHeight * ratio;
    }
    
    public override double GetWidth(double size)
    {
        var metrics = _typeface!.GetDesignGlyphMetrics(new[] {(short)'W'}, false)[0];
        var metrics2 = _typeface!.Metrics;
        var ratio = size / metrics2.DesignUnitsPerEm;
        return Math.Ceiling(metrics.AdvanceWidth * ratio);
    }

    public override double GetGlyphOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException($"TextAlignment {alignment} is not yet supported.");
        }

        return GetCapHeight(size);
    }

    public override double GetTextOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException($"TextAlignment {alignment} is not yet supported.");
        }

        var metrics = _typeface!.Metrics;
        var ratio = size / metrics.DesignUnitsPerEm;
        var descent = metrics.Descent * ratio;
        return -descent;
    }

    protected override bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex)
    {
        var indices = _typeface!.GetGlyphIndices(new[] {codePoint});
        glyphIndex = (ushort)indices[0];
        return true;
    }

    public override void Dispose()
    {
        _factory.Dispose();
        _factory = null!;
        _typeface?.Dispose();
        _typeface = null!;

        base.Dispose();
    }
}