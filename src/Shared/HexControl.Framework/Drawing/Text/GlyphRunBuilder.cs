using HexControl.Framework.Host;

namespace HexControl.Framework.Drawing.Text;

internal class GlyphRunBuilder : ITextBuilder
{
    private readonly double _characterWidth;
    private readonly List<(ISharedBrush Brush, SharedGlyphRun GlyphRun)> _glyphRuns;

    private int _index;
    private SharedPoint? _point;

    public GlyphRunBuilder(IGlyphTypeface typeface, double size, double characterWidth)
    {
        Typeface = typeface;
        Size = size;

        _characterWidth = characterWidth;
        _glyphRuns = new List<(ISharedBrush Brush, SharedGlyphRun GlyphRun)>();
    }
    
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

        _point = point with {Y = alignedY};
    }

    public void Draw(IRenderContext context)
    {
        foreach (var (brush, glyphRun) in _glyphRuns)
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
        for (var i = 0; i < _glyphRuns.Count; i++)
        {
            var run = _glyphRuns[i];
            if (run.Brush.Equals(brush))
            {
                return run.GlyphRun;
            }
        }
        
        var newGlyphRun = new SharedGlyphRun(Typeface, Size, new SharedPoint(0, 0));
        _glyphRuns.Add((brush, newGlyphRun));
        return newGlyphRun;
    }
}