using HexControl.Framework.Host;

namespace HexControl.Framework.Drawing.Text;

internal interface ITextBuilder
{
    IGlyphTypeface Typeface { get; }
    double Size { get; }

    void Whitespace(int count = 1);

    void Add(ISharedBrush brush, char @char);

    void Add(ISharedBrush brush, ReadOnlySpan<char> @string, int start = 0)
    {
        for (var i = start; i < @string.Length; i++)
        {
            Add(brush, @string[i]);
        }
    }

    void Next(SharedPoint point) => Next(point, TextAlignment.Top);

    void Next(SharedPoint point, TextAlignment alignment);

    void Clear();

    void Draw(IRenderContext context);
}