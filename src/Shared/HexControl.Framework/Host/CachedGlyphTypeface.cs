using HexControl.Framework.Drawing.Text;

namespace HexControl.Framework.Host;

internal abstract class CachedGlyphTypeface<TTypeface> : IGlyphTypeface
{
    private int[] _glyphCache;

    protected CachedGlyphTypeface()
    {
        _glyphCache = Array.Empty<int>();
    }

    public abstract TTypeface Typeface { get; }

    public abstract double GetCapHeight(double size);
    public abstract double GetWidth(double size);

    public virtual double GetGlyphOffsetY(TextAlignment alignment, double size) => 0;
    public virtual double GetTextOffsetY(TextAlignment alignment, double size) => 0;

    public bool TryGetGlyphIndex(int codePoint, out ushort glyphIndex)
    {
        if (_glyphCache.Length is 0)
        {
            _glyphCache = new int[ushort.MaxValue - 1];
        }

        var cachedGlyphIndex = _glyphCache[codePoint];
        if (cachedGlyphIndex is not 0)
        {
            glyphIndex = (ushort)cachedGlyphIndex;
            return cachedGlyphIndex is not -1;
        }

        var success = TryGetGlyphIndexInternal(codePoint, out glyphIndex);
        _glyphCache[codePoint] = success ? glyphIndex : -1;
        return success;
    }

    public virtual void Dispose() { }

    protected abstract bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex);
}