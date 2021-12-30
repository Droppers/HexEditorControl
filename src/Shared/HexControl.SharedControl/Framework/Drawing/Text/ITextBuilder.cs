using HexControl.SharedControl.Framework.Host.Typeface;

namespace HexControl.SharedControl.Framework.Drawing.Text;

internal interface ITextBuilder
{
    IGlyphTypeface Typeface { get; }
    double Size { get; }

    void Whitespace(int count = 1);

    void Add(ISharedBrush brush, char @char);

    void Add(ISharedBrush brush, ReadOnlySpan<char> @string)
    {
        foreach (var @char in @string)
        {
            Add(brush, @char);
        }
    }

    void Next(SharedPoint point) => Next(point, TextAlignment.Top);

    void Next(SharedPoint point, TextAlignment alignment);

    void Clear();

    void Draw(IRenderContext context);
}