using System.Text;
using HexControl.Framework.Host;
using HexControl.Framework.Optimizations;

namespace HexControl.Framework.Drawing.Text;

internal class TextLayoutBuilder : ITextBuilder
{
    private readonly List<(ISharedBrush brush, SharedPoint position, StringBuilder builder)> _builders;

    private readonly List<SharedTextLayout> _layouts;

    private int _index;

    private SharedPoint _startPosition;

    public TextLayoutBuilder(IGlyphTypeface typeface, double size)
    {
        Typeface = typeface;
        Size = size;

        _startPosition = new SharedPoint(0, 0);
        _layouts = new List<SharedTextLayout>();
        _builders = new List<(ISharedBrush brush, SharedPoint position, StringBuilder builder)>();
    }

    public IGlyphTypeface Typeface { get; }
    public double Size { get; }

    public void Add(ISharedBrush brush, char @char)
    {
        const char whiteSpace = ' ';

        var createBuilder = true;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < _builders.Count; i++)
        {
            var (builderBrush, _, builder) = _builders[i];

            if (ReferenceEquals(brush, builderBrush))
            {
                createBuilder = false;
                builder.Append(@char);
            }
            else
            {
                builder.Append(whiteSpace);
            }
        }

        if (createBuilder)
        {
            var builder = ObjectPool<StringBuilder>.Shared.Rent();
            builder.Clear();
            builder.Append(@char);

            var position = _startPosition with {X = _startPosition.X + _index * 8};
            _builders.Add((brush, position, builder));
        }

        _index++;
    }

    public void Whitespace(int count = 1)
    {
        const char whiteSpace = ' ';

        for (var i = 0; i < _builders.Count * count; i++)
        {
            var builderIndex = i / count;
            var (_, _, builder) = _builders[builderIndex];

            builder.Append(whiteSpace); // pad
        }

        _index += count;
    }

    public void Next(SharedPoint point, TextAlignment alignment)
    {
        UpdateLastLayout();

        _builders.Clear();
        _index = 0;

        _startPosition = point;
    }

    public void Clear()
    {
        _layouts.Clear();
        _builders.Clear();
    }

    public void Draw(IRenderContext context)
    {
        UpdateLastLayout();

        foreach (var layout in _layouts)
        {
            context.DrawTextLayout(layout.Brush, layout);
        }
    }

    private void UpdateLastLayout()
    {
        if (_builders.Count <= 0)
        {
            return;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < _builders.Count; i++)
        {
            var (brush, position, builder) = _builders[i];

            try
            {
                _layouts.Add(new SharedTextLayout(Typeface, Size, position)
                {
                    Text = builder.ToString(),
                    Brush = brush
                });
            }
            finally
            {
                ObjectPool<StringBuilder>.Shared.Return(builder);
            }
        }
    }
}