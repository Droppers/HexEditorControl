using HexControl.Framework.Drawing.Text;

namespace HexControl.Framework.Host.Typeface;

internal interface IGlyphTypeface : IDisposable
{
    bool TryGetGlyphIndex(int codePoint, out ushort glyphIndex);

    double GetCapHeight(double size);
    double GetWidth(double width);

    double GetGlyphOffsetY(TextAlignment alignment, double size);
    double GetTextOffsetY(TextAlignment alignment, double size);
}