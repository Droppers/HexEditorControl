using HexControl.SharedControl.Framework.Host.Typeface;

namespace HexControl.SharedControl.Framework.Drawing.Text;

internal class SharedTextLayout
{
    private SharedPoint _position;

    public SharedTextLayout(IGlyphTypeface typeface, double size, string text) : this(typeface, size,
        new SharedPoint(0, 0))
    {
        Text = text;
    }

    public SharedTextLayout(IGlyphTypeface typeface, double size, SharedPoint position)
    {
        Typeface = typeface;
        Size = size;
        Position = position;
        BrushRanges = new List<BrushRange>();
    }

    public IGlyphTypeface Typeface { get; }
    public double Size { get; }

    public SharedPoint Position
    {
        get => _position;
        set => _position = new SharedPoint(value.X, Typeface.GetTextOffsetY(TextAlignment.Top, Size) + value.Y);
    }

    public ISharedBrush? Brush { get; set; }
    public string Text { get; set; } = "";

    public List<BrushRange> BrushRanges { get; init; }

    public void AddRange(BrushRange range)
    {
        BrushRanges.Add(range);
    }

    internal record struct BrushRange(ISharedBrush Brush, int Start)
    {
        public int Length { get; set; } = 1;
    }
}