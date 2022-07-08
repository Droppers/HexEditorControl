using HexControl.Framework.Host;

namespace HexControl.Framework.Drawing.Text;

internal class SharedGlyphRun
{
    public SharedGlyphRun(IGlyphTypeface typeface, double fontSize, SharedPoint position)
    {
        Typeface = typeface;
        FontSize = fontSize;
        Position = position;

        GlyphIndices = new List<ushort>();
        AdvanceWidths = new List<double>();
        GlyphOffsets = new List<SharedPoint>();
    }

    public bool Empty => GlyphIndices.Count == 0;

    public double FontSize { get; }
    public IGlyphTypeface Typeface { get; }
    public SharedPoint Position { get; set; }
    public List<ushort> GlyphIndices { get; }
    public List<double> AdvanceWidths { get; }
    public List<SharedPoint> GlyphOffsets { get; }

    public void Write(SharedPoint point, char @char)
    {
        if (!Typeface.TryGetGlyphIndex(@char, out var glyphIndex))
        {
            _ = Typeface.TryGetGlyphIndex('.', out glyphIndex);
        }

        GlyphIndices.Add(glyphIndex);
        AdvanceWidths.Add(0);
        GlyphOffsets.Add(point);
    }

    public void WriteString(SharedPoint point, string str)
    {
        const char whiteSpace = ' ';

        var x = 0;
        foreach (var @char in str)
        {
            Write(new SharedPoint(point.X + x, point.Y), @char);

            x += (int)Typeface.GetWidth(FontSize) / (@char == whiteSpace ? 2 : 1);
        }
    }

    public void Clear()
    {
        GlyphIndices.Clear();
        AdvanceWidths.Clear();
        GlyphOffsets.Clear();
    }
}