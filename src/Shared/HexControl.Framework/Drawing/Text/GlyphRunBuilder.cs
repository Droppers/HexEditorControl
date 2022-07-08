using HexControl.Framework.Host;

namespace HexControl.Framework.Drawing.Text;

internal class GlyphRunBuilder : ITextBuilder
{
    private readonly double _characterWidth;
    private readonly Dictionary<ISharedBrush, SharedGlyphRun> _glyphRuns;

    private int _index;
    private SharedPoint? _point;

    public GlyphRunBuilder(IGlyphTypeface typeface, double size, double characterWidth)
    {
        _characterWidth = characterWidth;
        Typeface = typeface;
        Size = size;
        _glyphRuns = new Dictionary<ISharedBrush, SharedGlyphRun>();
    }

    public IReadOnlyDictionary<ISharedBrush, SharedGlyphRun> Entries => _glyphRuns;

    public SharedGlyphRun this[ISharedBrush brush] => GetGlyphRun(brush);

    public double Size { get; }

    public IGlyphTypeface Typeface { get; }

    public void Add(ISharedBrush brush, char @char)
    {
        const char spaceChar = ' ';

        if (_point is not { } point)
        {
            throw new InvalidOperationException("Call Next(SharedPoint) before adding characters to the builder.");
        }

        if (@char is not spaceChar)
        {
            var data = GetGlyphRun(brush);
            data.Write(new SharedPoint(point.X + _index * _characterWidth, point.Y), @char);
        }

        _index++;
    }

    public void Whitespace(int count)
    {
        _index += count;
    }

    public void Next(SharedPoint point, TextAlignment alignment)
    {
        _index = 0;

        var alignedY = point.Y + alignment switch
        {
            TextAlignment.Top => Typeface.GetGlyphOffsetY(alignment, Size),
            _ => throw new NotSupportedException($"TextAlignment {alignment} is not supported.")
        };

        _point = new SharedPoint(point.X, alignedY);
    }

    public void Draw(IRenderContext context)
    {
        foreach (var (brush, glyphRun) in Entries)
        {
            if (glyphRun.Empty)
            {
                continue;
            }

            context.DrawGlyphRun(brush, glyphRun);
        }
    }

    public void Clear()
    {
        foreach (var (_, data) in _glyphRuns)
        {
            data.Clear();
        }
    }

    private SharedGlyphRun GetGlyphRun(ISharedBrush brush)
    {
        if (_glyphRuns.TryGetValue(brush, out var glyphRun))
        {
            return glyphRun;
        }

        var newGlyphRun = new SharedGlyphRun(Typeface, Size, new SharedPoint(0, 0));
        _glyphRuns[brush] = newGlyphRun;
        return newGlyphRun;
    }
}