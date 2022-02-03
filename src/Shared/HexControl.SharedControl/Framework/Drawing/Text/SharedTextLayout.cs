using HexControl.SharedControl.Framework.Host.Typeface;

namespace HexControl.SharedControl.Framework.Drawing.Text;

internal class SharedTextLayout
{
    private readonly List<BrushRange> _brushRanges;

    public SharedTextLayout(IGlyphTypeface typeface, double size, string text) : this(typeface, size, new SharedPoint(0, 0))
    {
        Text = text;
    }

    public SharedTextLayout(IGlyphTypeface typeface, double size, SharedPoint position)
    {
        Typeface = typeface;
        Size = size;
        Position = position;
        _brushRanges = new List<BrushRange>();
    }

    public IGlyphTypeface Typeface { get; }
    public double Size { get; }
    public SharedPoint Position { get; set; }

    public ISharedBrush? Brush { get; set; }
    public string Text { get; set; } = "";
    public List<BrushRange> BrushRanges
    {
        get => _brushRanges;
        init => _brushRanges = value;
    }

    public void AddRange(BrushRange range)
    {
        _brushRanges.Add(range);
    }

    internal record struct BrushRange(ISharedBrush Brush, int Start)
    {
        public int Length { get; set; } = 1;
    }
}