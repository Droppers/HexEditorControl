using HexControl.SharedControl.Control.Helpers;
using HexControl.Framework.Drawing;
using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Visual;
using DocumentConfiguration = HexControl.SharedControl.Documents.DocumentConfiguration;

namespace HexControl.SharedControl.Control.Elements;

internal class OffsetColumn : VisualElement
{
    private readonly char[] _characterBuffer = new char[20];

    private readonly SharedHexControl _parent;

    private long _length;

    private int _zeroPadCount;

    public OffsetColumn(SharedHexControl parent)
    {
        _parent = parent;
    }

    public override double Width => (_zeroPadCount + 2) * _parent.CharacterWidth;

    public long Length
    {
        get => _length;
        set
        {
            _length = value;
            _zeroPadCount = Math.Max(_parent.OffsetHeader.Length,
                BaseConverter.Convert(value, Configuration.OffsetBase, true, _characterBuffer));
        }
    }

    public long Offset { get; set; }
    public DocumentConfiguration Configuration { get; set; } = DocumentConfiguration.Default;

    public ITextBuilder? TextBuilder { get; set; }

    protected override void Render(IRenderContext context)
    {
        if (TextBuilder == null)
        {
            return;
        }

        TextBuilder.Clear();
        WriteOffsetHeader(TextBuilder);
        WriteContentOffsets(TextBuilder);
        TextBuilder.Draw(context);
    }

    private void WriteOffsetHeader(ITextBuilder builder)
    {
        builder.Next(new SharedPoint(0, 0));
        builder.Add(_parent.HeaderForeground, _parent.OffsetHeader);
    }

    private void WriteContentOffsets(ITextBuilder builder)
    {
        var rowCount = Height / _parent.RowHeight;
        var glyphMiddleOffset = (int)Math.Round(_parent.RowHeight / 2d - _parent.CharacterHeight / 2d);

        for (var row = 0; row < rowCount; row++)
        {
            var y = row * _parent.RowHeight + glyphMiddleOffset + _parent.HeaderHeight;

            var offset = Offset + row * Configuration.BytesPerRow;
            builder.Next(new SharedPoint(0, y));

            var length = BaseConverter.Convert(offset, Configuration.OffsetBase, true, _characterBuffer);
            var padZeros = _zeroPadCount - length;
            for (var i = 0; i < length + padZeros; i++)
            {
                var charIndex = length - (i - padZeros) - 1;
                var @char = charIndex >= length ? '0' : _characterBuffer[charIndex];
                builder.Add(_parent.OffsetForeground, @char);
            }

            if (y > Height || offset + Configuration.BytesPerRow >= Length)
            {
                break;
            }
        }
    }
}