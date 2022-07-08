using HexControl.Framework.Drawing.Text;

namespace HexControl.Framework.Host;

internal abstract class CachedGlyphTypeface<TTypeface> : IGlyphTypeface
{
    private readonly Dictionary<int, ushort> _glyphCache;

    protected CachedGlyphTypeface()
    {
        _glyphCache = new Dictionary<int, ushort>();
    }

    public abstract TTypeface Typeface { get; }

    public abstract double GetCapHeight(double size);
    public abstract double GetWidth(double size);

    public virtual double GetGlyphOffsetY(TextAlignment alignment, double size) => 0;
    public virtual double GetTextOffsetY(TextAlignment alignment, double size) => 0;

    public bool TryGetGlyphIndex(int codePoint, out ushort glyphIndex)
    {
        if (_glyphCache.TryGetValue(codePoint, out var cachedGlyphIndex))
        {
            glyphIndex = cachedGlyphIndex;
            return cachedGlyphIndex != 0;
        }

        var success = TryGetGlyphIndexInternal(codePoint, out glyphIndex);
        _glyphCache[codePoint] = success ? glyphIndex : (ushort)0;
        return success;
    }

    public virtual void Dispose() { }

    public abstract bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex);
}